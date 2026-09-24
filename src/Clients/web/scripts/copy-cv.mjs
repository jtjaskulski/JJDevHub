import { copyFileSync, existsSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');
const contentDir = join(root, 'src', 'content');
const localPath = join(contentDir, 'cv.local.json');
const examplePath = join(contentDir, 'cv.example.json');
const destPath = join(contentDir, 'cv.json');

const sourcePath = existsSync(localPath) ? localPath : examplePath;
const sourceName = existsSync(localPath) ? 'cv.local.json' : 'cv.example.json';

copyFileSync(sourcePath, destPath);
console.log(`Copied ${sourceName} → cv.json`);
