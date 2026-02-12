pipeline {
    agent any
    environment {
        DOTNET_SDK = '8.0'
    }
    stages {
        stage('Checkout') {
            steps { checkout scm }
        }
        stage('Restore') {
            steps { sh 'dotnet restore JJDevHub.sln' }
        }
        stage('Build') {
            steps { sh 'dotnet build JJDevHub.sln --configuration Release' }
        }
        stage('Static Analysis') {
            steps {
                echo 'Connecting to SonarQube...'
                // TODO: Add sonar-scanner command here
            }
        }
    }
}
