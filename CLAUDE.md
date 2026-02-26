# Parking

Serverless parking space allocation application on AWS.

## Architecture

- **Parking.Api** - ASP.NET Core Lambda (API Gateway HTTP API v2, Cognito JWT auth)
- **Parking.Service** - Lambda function for scheduled tasks (EventBridge triggers)
- **Parking.Business** - Business logic layer
- **Parking.Data** - Data access (DynamoDB, Cognito, SES, SNS)
- **Parking.Model** - Domain models

## Infrastructure

Terraform in `infrastructure/`. S3 backend with key `parking/{env}/terraform.tfstate`.

No `.tfvars` in source control. Variables provided via `-var` flags in CI or `TF_VAR_*` environment variables locally.

## Lambda Environment Variables

| Variable | Source |
|---|---|
| `TABLE_NAME` | DynamoDB table name |
| `USER_POOL_ID` | Cognito user pool ID |
| `TOPIC_NAME` | SNS topic ARN |
| `FROM_EMAIL_ADDRESS` | SES sender address |
| `SMTP_CONFIG_SET` | SES configuration set name |
| `CORS_ORIGIN` | Comma-separated allowed origins |

## Build & Deploy

- **CI/CD**: GitHub Actions (`.github/workflows/deploy.yml`) with OIDC role assumption
- **Lambda packaging**: `dotnet publish -c Release -r linux-x64` then zip
- **Tests**: `dotnet test` (uses Microsoft.Testing.Platform via `global.json`)
- **.NET version**: 10.0

## Key Conventions

- Environment variable access via `Helpers.GetRequiredEnvironmentVariable()` in `Parking.Data`
- SES hardcoded to `eu-west-1`; all other services in `eu-west-2`
- Cognito groups: `TeamLeader`, `UserAdmin`
