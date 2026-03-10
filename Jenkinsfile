pipeline {
    agent any

    options {
        buildDiscarder(logRotator(numToKeepStr: '10'))
        timestamps()
        timeout(time: 45, unit: 'MINUTES')
    }

    environment {
        DOTNET_CLI_TELEMETRY_OPTOUT = '1'
        DOTNET_NOLOGO = '1'
        SONAR_HOST_URL = 'http://jjdevhub-sonarqube:9000'
        SONAR_PROJECT_KEY = 'JJDevHub'
    }

    stages {
        // ───────────────────────────────────────────────────────
        // STAGE 1: RESTORE
        // ───────────────────────────────────────────────────────
        stage('Restore') {
            steps {
                sh 'dotnet restore JJDevHub.sln'
            }
        }

        // ───────────────────────────────────────────────────────
        // STAGE 2: BUILD
        // ───────────────────────────────────────────────────────
        stage('Build') {
            steps {
                sh 'dotnet build JJDevHub.sln --configuration Release --no-restore'
            }
        }

        // ───────────────────────────────────────────────────────
        // STAGE 3: UNIT TESTS
        // ───────────────────────────────────────────────────────
        stage('Unit Tests') {
            steps {
                sh '''dotnet test tests/JJDevHub.Content.UnitTests/JJDevHub.Content.UnitTests.csproj \
                    --configuration Release \
                    --no-build \
                    --logger "trx;LogFileName=unit-test-results.trx" \
                    --collect:"XPlat Code Coverage" \
                    --results-directory ./test-results/unit'''
            }
            post {
                always {
                    archiveArtifacts artifacts: 'test-results/unit/**/*', allowEmptyArchive: true
                }
            }
        }

        // ───────────────────────────────────────────────────────
        // STAGE 4: INTEGRATION TESTS (Testcontainers)
        // ───────────────────────────────────────────────────────
        stage('Integration Tests') {
            steps {
                sh '''dotnet test tests/JJDevHub.Content.IntegrationTests/JJDevHub.Content.IntegrationTests.csproj \
                    --configuration Release \
                    --no-build \
                    --logger "trx;LogFileName=integration-test-results.trx" \
                    --collect:"XPlat Code Coverage" \
                    --results-directory ./test-results/integration'''
            }
            post {
                always {
                    archiveArtifacts artifacts: 'test-results/integration/**/*', allowEmptyArchive: true
                }
            }
        }

        // ───────────────────────────────────────────────────────
        // STAGE 5: ANGULAR BUILD
        // ───────────────────────────────────────────────────────
        stage('Angular Build') {
            steps {
                dir('src/Clients/web') {
                    sh 'npm ci'
                    sh 'npx ng build --configuration production'
                }
            }
        }

        // ───────────────────────────────────────────────────────
        // STAGE 6: SONARQUBE ANALYSIS
        // ───────────────────────────────────────────────────────
        stage('SonarQube Analysis') {
            steps {
                withCredentials([string(credentialsId: 'sonarqube-token', variable: 'SONAR_TOKEN')]) {
                    sh '''dotnet tool install --global dotnet-sonarscanner || true

                    export PATH="$PATH:$HOME/.dotnet/tools"

                    dotnet-sonarscanner begin \
                        /k:"${SONAR_PROJECT_KEY}" \
                        /d:sonar.host.url="${SONAR_HOST_URL}" \
                        /d:sonar.token="${SONAR_TOKEN}" \
                        /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml" \
                        /d:sonar.cs.vstest.reportsPaths="**/*.trx" \
                        /d:sonar.coverage.exclusions="**/Migrations/**,**/Program.cs,**/DependencyInjection.cs" \
                        /d:sonar.exclusions="**/wwwroot/**,**/node_modules/**"

                    dotnet build JJDevHub.sln --configuration Release --no-restore

                    dotnet-sonarscanner end /d:sonar.token="${SONAR_TOKEN}"'''
                }
            }
        }

        // ───────────────────────────────────────────────────────
        // STAGE 7: QUALITY GATE
        // ───────────────────────────────────────────────────────
        stage('Quality Gate') {
            steps {
                script {
                    timeout(time: 5, unit: 'MINUTES') {
                        def qg = waitForQualityGate()
                        if (qg.status != 'OK') {
                            error "Pipeline aborted: SonarQube Quality Gate status is ${qg.status}"
                        }
                    }
                }
            }
        }

        // ───────────────────────────────────────────────────────
        // STAGE 8: DOCKER BUILD (all services in parallel)
        // ───────────────────────────────────────────────────────
        stage('Docker Build') {
            parallel {
                stage('Content API') {
                    steps {
                        sh '''docker build \
                            -f src/Services/JJDevHub.Content/JJDevHub.Content.Api/Dockerfile \
                            -t jjdevhub-content-api:${BUILD_NUMBER} \
                            -t jjdevhub-content-api:latest \
                            .'''
                    }
                }
                stage('Analytics API') {
                    steps {
                        sh '''docker build \
                            -f src/Services/JJDevHub.Analytics/JJDevHub.Analytics.Api/Dockerfile \
                            -t jjdevhub-analytics-api:${BUILD_NUMBER} \
                            -t jjdevhub-analytics-api:latest \
                            .'''
                    }
                }
                stage('Identity API') {
                    steps {
                        sh '''docker build \
                            -f src/Services/JJDevHub.Identity/Dockerfile \
                            -t jjdevhub-identity-api:${BUILD_NUMBER} \
                            -t jjdevhub-identity-api:latest \
                            .'''
                    }
                }
                stage('AI Gateway') {
                    steps {
                        sh '''docker build \
                            -f src/Services/JJDevHub.AI.Gateway/JJDevHub.AI.Gateway/Dockerfile \
                            -t jjdevhub-ai-gateway:${BUILD_NUMBER} \
                            -t jjdevhub-ai-gateway:latest \
                            .'''
                    }
                }
                stage('Notification API') {
                    steps {
                        sh '''docker build \
                            -f src/Services/JJDevHub.Notification/Dockerfile \
                            -t jjdevhub-notification-api:${BUILD_NUMBER} \
                            -t jjdevhub-notification-api:latest \
                            .'''
                    }
                }
                stage('Education API') {
                    steps {
                        sh '''docker build \
                            -f src/Services/JJDevHub.Education/Dockerfile \
                            -t jjdevhub-education-api:${BUILD_NUMBER} \
                            -t jjdevhub-education-api:latest \
                            .'''
                    }
                }
                stage('Sync Worker') {
                    steps {
                        sh '''docker build \
                            -f src/Services/JJDevHub.Sync/Dockerfile \
                            -t jjdevhub-sync-worker:${BUILD_NUMBER} \
                            -t jjdevhub-sync-worker:latest \
                            .'''
                    }
                }
                stage('Angular Web') {
                    steps {
                        sh '''docker build \
                            -f src/Clients/web/Dockerfile \
                            -t jjdevhub-angular:${BUILD_NUMBER} \
                            -t jjdevhub-angular:latest \
                            src/Clients/web'''
                    }
                }
            }
        }

        // ───────────────────────────────────────────────────────
        // STAGE 9: DEPLOY (Docker Compose)
        // ───────────────────────────────────────────────────────
        stage('Deploy') {
            steps {
                dir('infra/docker') {
                    sh 'docker-compose up -d --remove-orphans'
                }
            }
        }
    }

    post {
        always {
            cleanWs()
        }
        success {
            echo 'Pipeline completed successfully!'
        }
        failure {
            echo 'Pipeline failed. Check logs above.'
        }
    }
}
