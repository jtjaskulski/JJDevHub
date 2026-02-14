pipeline {
    agent any
    
    options {
        buildDiscarder(logRotator(numToKeepStr: '10'))
        timestamps()
    }

    environment {
        // Define common environment variables here if needed
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
    }

    stages {
        stage('Detect Changes') {
            steps {
                script {
                    // Check for changes in specific directories to optimize build
                    if (fileExists('.git')) {
                        echo "Git repository detected. Checking changes..."
                        // Returns 0 if found (true), 1 if not found (false). We invert logic for variable assignment if needed or just check exit code.
                        // Grep returns 0 if match found.
                        env.CONTENT_CHANGED = sh(script: "git diff --name-only HEAD~1 HEAD | grep 'src/Services/JJDevHub.Content' || true", returnStdout: true).trim() ? 'true' : 'false'
                        env.IDENTITY_CHANGED = sh(script: "git diff --name-only HEAD~1 HEAD | grep 'src/Services/JJDevHub.Identity' || true", returnStdout: true).trim() ? 'true' : 'false'
                        env.EDUCATION_CHANGED = sh(script: "git diff --name-only HEAD~1 HEAD | grep 'src/Services/JJDevHub.Education' || true", returnStdout: true).trim() ? 'true' : 'false'
                        env.WEB_CHANGED = sh(script: "git diff --name-only HEAD~1 HEAD | grep 'src/Clients/web' || true", returnStdout: true).trim() ? 'true' : 'false'
                        
                        echo "Changes detected: Content=${env.CONTENT_CHANGED}, Identity=${env.IDENTITY_CHANGED}, Education=${env.EDUCATION_CHANGED}, Web=${env.WEB_CHANGED}"
                    } else {
                        echo "No git repository found (or first run). Building everything."
                        env.CONTENT_CHANGED = 'true'
                        env.IDENTITY_CHANGED = 'true'
                        env.EDUCATION_CHANGED = 'true'
                        env.WEB_CHANGED = 'true'
                    }
                }
            }
        }

        stage('Build & Test') {
            parallel {
                stage('Backend - Content') {
                    when { expression { env.CONTENT_CHANGED == 'true' || env.BUILD_ALL == 'true' } }
                    steps {
                        dir('src/Services/JJDevHub.Content') {
                            // Assuming project structure. Adjust path to csproj if needed
                            sh 'dotnet restore'
                            sh 'dotnet build --configuration Release --no-restore'
                            // sh 'dotnet test --no-build --configuration Release'
                        }
                    }
                }
                stage('Backend - Identity') {
                    when { expression { env.IDENTITY_CHANGED == 'true' || env.BUILD_ALL == 'true' } }
                    steps {
                        dir('src/Services/JJDevHub.Identity') {
                            sh 'dotnet restore'
                            sh 'dotnet build --configuration Release --no-restore'
                        }
                    }
                }
                 stage('Backend - Education') {
                    when { expression { env.EDUCATION_CHANGED == 'true' || env.BUILD_ALL == 'true' } }
                    steps {
                        dir('src/Services/JJDevHub.Education') {
                            sh 'dotnet restore'
                            sh 'dotnet build --configuration Release --no-restore'
                        }
                    }
                }
                stage('Frontend - Angular') {
                    when { expression { env.WEB_CHANGED == 'true' || env.BUILD_ALL == 'true' } }
                    steps {
                        dir('src/Clients/web') {
                            sh 'npm install'
                            sh 'npm run build -- --configuration production'
                        }
                    }
                }
            }
        }

        stage('Security Scan') {
            steps {
                echo 'Checking secrets in HashiCorp Vault...'
                // Placeholder for Vault integration
                // withVault(...) { ... }
            }
        }

        stage('Deploy Local Docker') {
            steps {
                // Ensure docker-compose exists
                sh 'docker-compose up -d --build --remove-orphans'
            }
        }
    }
}
