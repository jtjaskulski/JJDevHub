# Jenkins Tutorial & Best Practices for JJDevHub

## 1. Jenkinsfile Location: Root vs. Infra

You asked: *"Jenkinsfile nie powinien być wewnątrz infra?"*

**Short Answer:** It is standard practice to keep the `Jenkinsfile` in the **root** of the repository.

**Why?**
1.  **Automatic Discovery:** Jenkins "Multibranch Pipeline" and "Organization Folder" jobs automatically scan the repository root for a `Jenkinsfile`. If it's there, Jenkins automatically creates a pipeline for that branch.
2.  **Convention:** Most CI/CD tools (GitHub Actions `.github/workflows`, GitLab CI `.gitlab-ci.yml`, Azure DevOps `azure-pipelines.yml`) expect configuration at the root or a standard location.
3.  **Simplicity:** Developers know exactly where to look for the build definition.

**Can it be in `infra/`?**
Yes. If you create a "Pipeline" job manually, you can specify any path (e.g., `infra/jenkins/Jenkinsfile`). However, you lose the automatic branch discovery features of Multibranch Pipelines unless you configure custom script paths (which is possible but adds friction).

---

## 2. Setting up Jenkins Locally (Docker)

To run Jenkins locally and build this project, we will use Docker. This approach avoids installing Java/Jenkins directly on your Windows machine.

### Prerequisites in `infra/docker/`

We need a `docker-compose` setup for Jenkins that has:
1.  **Blue Ocean Plugin**: For a better UI.
2.  **Docker-in-Docker (DinD)**: So Jenkins can run `docker` commands (required for your current pipeline).
3.  **Dotnet SDK**: Or the ability to install it. *Note: Your current Jenkinsfile implies using agents or having dotnet installed on the master. For simplicity, we will run Jenkins with Docker socket mounted.*

### Step 1: Jenkins Docker Compose

Edit the existing `infra/docker/jenkins-compose.yml` (or the Jenkins service in `docker-compose.yml`). The relevant configuration:

```yaml
version: '3.8'

services:
  jenkins:
    image: jenkins/jenkins:lts-jdk17
    container_name: jenkins-local
    privileged: true          # ⚠ grants full host access — LOCAL DEV ONLY
    user: root                # ⚠ needed for Docker socket — LOCAL DEV ONLY
    ports:
      - 8080:8080
      - 50000:50000
    volumes:
      - jenkins_home:/var/jenkins_home
      - /var/run/docker.sock:/var/run/docker.sock  # ⚠ host Docker — LOCAL DEV ONLY
    environment:
      - DOCKER_HOST=unix:///var/run/docker.sock

volumes:
  jenkins_home:
```

> **Security note:** Running Jenkins as `root` with `privileged: true` and the host Docker socket mounted gives Jenkins full root-level access to the host. This setup is for **local development only**. For production or shared environments, use a dedicated build agent, Docker-in-Docker (DinD) sidecar, or rootless Docker.

### Step 2: Run Jenkins

Open a terminal in `infra/docker` and run:

```bash
docker-compose -f jenkins-compose.yml up -d
```

### Step 3: Unlock Jenkins

1.  Open [http://localhost:8080](http://localhost:8080).
2.  It will ask for an "Administrator password".
3.  In your terminal, run:
    ```bash
    docker exec jenkins-local cat /var/jenkins_home/secrets/initialAdminPassword
    ```
4.  Copy the password and paste it into the browser.
5.  Select **"Install suggested plugins"**.
6.  Create your first admin user.

### Step 4: Install Required Plugins

Your `Jenkinsfile` uses Docker and Git.
1.  Go to **Manage Jenkins > Plugins**.
2.  Install:
    *   **Docker Pipeline**
    *   **Docker**
    *   **Pipeline: Stage View** (usually installed by default)

### Step 5: Configure the Job

Since this is a local setup, the trickiest part is letting Jenkins access your Windows Git repository.

**Option A: Push to GitHub/GitLab (Easiest)**
1.  Push your code to a remote repository.
2.  In Jenkins, click **New Item** > **Pipeline**.
3.  Name it `JJDevHub`.
4.  Scroll to **Pipeline** section.
5.  Definition: **Pipeline script from SCM**.
6.  SCM: **Git**.
7.  Repository URL: `https://github.com/jtjaskulski/JJDevHub.git` (or your URL).
8.  Script Path: `Jenkinsfile`.
9.  Save and click **Build Now**.

**Option B: Local File System (Advanced)**
If you don't want to push to a remote:
1.  You need to mount your source code into the Jenkins container in `jenkins-compose.yml`.
    ```yaml
    volumes:
      - ../../:/var/jenkins_git_repo  # Mounts repo root (from infra/docker/) to /var/jenkins_git_repo
    ```
2.  In Jenkins, use "Pipeline script from SCM" -> Git -> Repository URL: `file:///var/jenkins_git_repo`.

## 3. Understanding the Pipeline

The `Jenkinsfile` has 9 stages:

1.  **Restore** - `dotnet restore JJDevHub.sln`
2.  **Build** - `dotnet build --configuration Release`
3.  **Unit Tests** - Content unit tests with coverage (34 tests)
4.  **Integration Tests** - Content integration tests with Testcontainers (PostgreSQL + MongoDB)
5.  **Angular Build** - `npm ci` + `npx ng build --configuration production`
6.  **SonarQube Analysis** - Static code analysis with coverage reports
7.  **Quality Gate** - Waits for SonarQube quality gate (blocks if failed)
8.  **Docker Build** - Builds **8 images in parallel**:
    *   Content API, Analytics API, Identity API, AI Gateway
    *   Notification API, Education API, Sync Worker, Angular Web
9.  **Deploy** - `docker-compose up -d --remove-orphans` in `infra/docker/`

*Note: Docker build uses host Docker via `/var/run/docker.sock` mount.*

## 4. Troubleshooting Common Issues

*   **"dotnet command not found"**: The standard Jenkins image doesn't have .NET installed. You either need to:
    *   Use a Jenkins agent that has .NET.
    *   Or install .NET in the Jenkins container (Dockerfile approach).
    *   Or run the build steps inside Docker containers (e.g., `agent { docker { image 'mcr.microsoft.com/dotnet/sdk:8.0' } }`). **<-- Recommended for your setup.**

### Updating Jenkinsfile for Docker Agents

To avoid installing .NET on the Jenkins master, change your `Jenkinsfile` to run steps inside a Docker container:

```groovy
pipeline {
    agent none // Don't allocate a node globally yet

    stages {
        stage('Build Backend') {
            agent {
                docker { image 'mcr.microsoft.com/dotnet/sdk:8.0' }
            }
            steps {
                sh 'dotnet build ...'
            }
        }
    }
}
```

This makes your pipeline portable and not dependent on what's installed on the Jenkins server!
