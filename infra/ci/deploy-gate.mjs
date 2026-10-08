import { appendFileSync, realpathSync } from 'node:fs'
import { pathToFileURL } from 'node:url'

const WORKFLOWS = [
  { name: 'api', file: 'api.yml' },
  { name: 'web', file: 'web.yml' },
]
const API = 'https://api.github.com'

// GitHub path filters: `*` does not cross `/`, `**` does, and `**/` may match
// zero directories (`docs/**/*.md` matches `docs/README.md`). `?` and `+`
// quantify the preceding atom. The pattern matches the whole path.
export function matchGithubPath(pattern, file) {
  return githubGlobToRegExp(pattern).test(file)
}

export function fileMatchesPaths(patterns, file) {
  let matched = false
  for (const pattern of patterns) {
    if (typeof pattern !== 'string' || pattern === '') {
      throw new Error('deploy: empty path pattern')
    }
    const negated = pattern.startsWith('!')
    const body = negated ? pattern.slice(1) : pattern
    if (body === '') throw new Error(`deploy: invalid path pattern ${pattern}`)
    if (!matchGithubPath(body, file)) continue
    matched = !negated
  }
  return matched
}

// `null` means `on.push` has no `paths` filter, so the workflow always runs.
export function parsePushPaths(yaml) {
  const lines = String(yaml).replace(/\r\n/g, '\n').replace(/\r/g, '\n').split('\n')
  let section = 'root'
  let onIndent = 0
  let pushIndent = 0
  let pathsIndent = 0
  let sawOn = false
  let sawPush = false
  let sawPaths = false
  const paths = []

  for (let index = 0; index < lines.length; index++) {
    const raw = lines[index]
    if (raw.trim() === '' || raw.trim().startsWith('#')) continue
    if (/^\t/.test(raw)) throw new Error(`workflow:${index + 1}: tabs are not allowed`)
    const indent = raw.match(/^ */)?.[0].length ?? 0
    const text = raw.trim()
    const line = index + 1

    if (section === 'root') {
      if (text === 'on:') {
        section = 'on'
        onIndent = indent
        sawOn = true
      }
      continue
    }

    if (section === 'on') {
      if (indent <= onIndent) {
        section = 'root'
        continue
      }
      if (text === 'push:') {
        section = 'push'
        pushIndent = indent
        sawPush = true
      }
      continue
    }

    if (section === 'push') {
      if (indent <= pushIndent) {
        section = indent <= onIndent ? 'root' : 'on'
        continue
      }
      if (text === 'paths:') {
        section = 'paths'
        pathsIndent = indent
        sawPaths = true
        continue
      }
      if (text.startsWith('paths:')) {
        throw new Error(`workflow:${line}: push.paths must be a block list`)
      }
      continue
    }

    if (indent <= pathsIndent) {
      if (indent <= pushIndent) section = indent <= onIndent ? 'root' : 'on'
      else section = 'push'
      continue
    }

    const item = text.match(/^-\s+(?:"([^"]*)"|'([^']*)'|(\S+))$/)
    if (!item) throw new Error(`workflow:${line}: expected a path entry`)
    paths.push(item[1] ?? item[2] ?? item[3])
  }

  if (!sawOn) throw new Error('workflow: missing on: block')
  if (!sawPush) throw new Error('workflow: missing on.push')
  if (!sawPaths) return null
  return paths
}

export function applicableWorkflows({ workflows, changedFiles, triggerName, filesTruncated = false }) {
  const names = []
  const seen = new Set()
  const add = (name) => {
    if (!name || seen.has(name)) return
    seen.add(name)
    names.push(name)
  }

  for (const workflow of workflows) {
    if (filesTruncated || workflow.paths == null) {
      add(workflow.name)
      continue
    }
    if (changedFiles.some((file) => fileMatchesPaths(workflow.paths, file))) add(workflow.name)
  }
  add(triggerName)
  return names
}

export function decide({ applicable, runsByWorkflow }) {
  const pending = []
  const failed = []

  if (applicable.length === 0) {
    return {
      deploy: false,
      wait: false,
      blocked: true,
      pending,
      failed: [{ name: '(none)', conclusion: 'no applicable workflow' }],
    }
  }

  for (const name of applicable) {
    const runs = runsByWorkflow[name] ?? []
    if (runs.length === 0) {
      pending.push(name)
      continue
    }
    const latest = latestRun(runs)
    if (latest.status !== 'completed') {
      pending.push(name)
      continue
    }
    if (latest.conclusion !== 'success') {
      failed.push({ name, conclusion: latest.conclusion ?? 'missing' })
    }
  }

  if (failed.length > 0) {
    return { deploy: false, wait: false, blocked: true, pending, failed }
  }
  if (pending.length > 0) {
    return { deploy: false, wait: true, blocked: false, pending, failed }
  }
  return { deploy: true, wait: false, blocked: false, pending, failed }
}

export async function main(env = process.env) {
  const sha = env.HEAD_SHA ?? ''
  const repo = env.REPO ?? ''
  const trigger = env.TRIGGER ?? ''
  const token = env.GH_TOKEN || env.GITHUB_TOKEN || ''
  if (!/^[0-9a-f]{40}$/i.test(sha)) throw new Error('deploy: HEAD_SHA must be a full commit sha')
  if (!/^[^/\s]+\/[^/\s]+$/.test(repo)) throw new Error('deploy: REPO must be owner/name')
  if (!token) throw new Error('deploy: GH_TOKEN is required')
  if (!WORKFLOWS.some((workflow) => workflow.name === trigger)) {
    throw new Error(`deploy: unknown triggering workflow ${trigger || '(empty)'}`)
  }

  const { files, truncated } = await listChangedFiles(repo, sha, token)
  const workflows = []
  for (const workflow of WORKFLOWS) {
    const yaml = await readWorkflow(repo, workflow.file, sha, token)
    workflows.push({ name: workflow.name, paths: parsePushPaths(yaml) })
  }

  const applicable = applicableWorkflows({
    workflows,
    changedFiles: files,
    triggerName: trigger,
    filesTruncated: truncated,
  })
  const runsByWorkflow = {}
  for (const name of applicable) {
    const workflow = WORKFLOWS.find((item) => item.name === name)
    runsByWorkflow[name] = await listRuns(repo, workflow.file, sha, token)
  }

  const decision = decide({ applicable, runsByWorkflow })
  const scope = applicable.join(', ')
  if (decision.blocked) {
    const detail = decision.failed.map((item) => `${item.name} ${item.conclusion}`).join(', ')
    throw new Error(`deploy: blocked for ${sha} (${scope}): ${detail}`)
  }
  if (decision.wait) {
    console.log(`deploy: waiting for ${decision.pending.join(', ')} on ${sha} (${scope})`)
    writeOutput(env, 'false')
    return decision
  }
  console.log(`deploy: ${scope} succeeded for ${sha}`)
  writeOutput(env, 'true')
  return decision
}

function githubGlobToRegExp(pattern) {
  if (typeof pattern !== 'string' || pattern === '') {
    throw new Error('deploy: empty path pattern')
  }
  let source = ''
  let index = 0
  while (index < pattern.length) {
    const atom = readAtom(pattern, index)
    index = atom.next
    if (pattern[index] === '?' || pattern[index] === '+') {
      source += `(?:${atom.source})${pattern[index]}`
      index += 1
      continue
    }
    source += atom.source
  }
  return new RegExp(`^${source}$`)
}

function readAtom(pattern, index) {
  if (pattern[index] === '\\') {
    if (index + 1 >= pattern.length) throw new Error(`deploy: trailing escape in ${pattern}`)
    return { source: escapeChar(pattern[index + 1]), next: index + 2 }
  }
  if (pattern.startsWith('**/', index)) return { source: '(?:.*/)?', next: index + 3 }
  if (pattern.startsWith('**', index)) return { source: '.*', next: index + 2 }
  if (pattern[index] === '*') return { source: '[^/]*', next: index + 1 }
  if (pattern[index] === '[') {
    const end = pattern.indexOf(']', index + 1)
    if (end === -1) throw new Error(`deploy: unclosed character class in ${pattern}`)
    return { source: `[${pattern.slice(index + 1, end)}]`, next: end + 1 }
  }
  if (pattern[index] === '?' || pattern[index] === '+') {
    throw new Error(`deploy: ${pattern[index]} has nothing to quantify in ${pattern}`)
  }
  return { source: escapeChar(pattern[index]), next: index + 1 }
}

function escapeChar(char) {
  return /[\\^$.*+?()[\]{}|]/.test(char) ? `\\${char}` : char
}

function latestRun(runs) {
  return runs.slice().sort((left, right) => {
    const byTime = Date.parse(right.created_at ?? '') - Date.parse(left.created_at ?? '')
    if (Number.isFinite(byTime) && byTime !== 0) return byTime
    return (right.id ?? 0) - (left.id ?? 0)
  })[0]
}

async function listChangedFiles(repo, sha, token) {
  const commit = await github(`/repos/${repo}/git/commits/${sha}`, token)
  const parent = commit.body.parents?.[0]?.sha
  if (!parent) return { files: [], truncated: true }

  // REST compare accepts only three-dot `base...head`. Diffing a commit
  // against its first parent matches the two-dot file list GitHub uses for
  // a one-commit push and for a merge commit.
  const files = []
  for (let page = 1; page <= 30; page++) {
    let result
    try {
      result = await github(`/repos/${repo}/compare/${parent}...${sha}?per_page=100&page=${page}`, token)
    } catch (error) {
      if (error.status === 406 || error.status === 422) return { files, truncated: true }
      throw error
    }
    const batch = result.body.files
    if (!Array.isArray(batch) || batch.length >= 300) return { files, truncated: true }
    for (const file of batch) {
      if (typeof file?.filename === 'string') files.push(file.filename)
    }
    const link = result.headers.get('link') ?? ''
    if (!link.includes('rel="next"')) return { files, truncated: false }
  }
  return { files, truncated: true }
}

async function readWorkflow(repo, file, sha, token) {
  const { body } = await github(`/repos/${repo}/contents/.github/workflows/${file}?ref=${sha}`, token)
  if (body.encoding !== 'base64' || typeof body.content !== 'string') {
    throw new Error(`deploy: cannot read .github/workflows/${file} at ${sha}`)
  }
  return Buffer.from(body.content, 'base64').toString('utf8')
}

async function listRuns(repo, file, sha, token) {
  const runs = []
  for (let page = 1; page <= 10; page++) {
    const { body, headers } = await github(
      `/repos/${repo}/actions/workflows/${file}/runs?head_sha=${sha}&per_page=100&page=${page}`,
      token,
    )
    if (!Array.isArray(body.workflow_runs)) {
      throw new Error(`deploy: workflow runs for ${file} at ${sha} were not a list`)
    }
    for (const run of body.workflow_runs) {
      runs.push({
        id: run.id,
        status: run.status,
        conclusion: run.conclusion,
        created_at: run.created_at,
      })
    }
    const link = headers.get('link') ?? ''
    if (!link.includes('rel="next"')) return runs
    if (page === 10) throw new Error(`deploy: too many runs for ${file} at ${sha}`)
  }
  return runs
}

async function github(path, token) {
  const response = await fetch(`${API}${path}`, {
    headers: {
      Accept: 'application/vnd.github+json',
      Authorization: `Bearer ${token}`,
      'X-GitHub-Api-Version': '2022-11-28',
      'User-Agent': 'jjdevhub-deploy-gate',
    },
  })
  const text = await response.text()
  let body = {}
  if (text) {
    try {
      body = JSON.parse(text)
    } catch {
      body = { message: text.slice(0, 300) }
    }
  }
  if (!response.ok) {
    const message = typeof body.message === 'string' ? body.message : response.statusText
    const error = new Error(`GitHub API ${response.status} ${path}: ${message}`)
    error.status = response.status
    throw error
  }
  return { body, headers: response.headers }
}

function writeOutput(env, deploy) {
  if (!env.GITHUB_OUTPUT) return
  appendFileSync(env.GITHUB_OUTPUT, `deploy=${deploy}\n`)
}

function invokedAsCli() {
  const entry = process.argv[1]
  if (!entry) return false
  try {
    return import.meta.url === pathToFileURL(realpathSync(entry)).href
  } catch {
    return false
  }
}

if (invokedAsCli()) {
  main().catch((error) => {
    console.error(error instanceof Error ? error.message : error)
    process.exit(1)
  })
}
