# OIDC provider is account-wide; only create in develop to avoid duplicates
resource "aws_iam_openid_connect_provider" "github" {
  count           = var.environment == "develop" ? 1 : 0
  url             = "https://token.actions.githubusercontent.com"
  client_id_list  = ["sts.amazonaws.com"]
  thumbprint_list = ["ffffffffffffffffffffffffffffffffffffffff"]
}

data "aws_iam_openid_connect_provider" "github" {
  count = var.environment == "prod" ? 1 : 0
  url   = "https://token.actions.githubusercontent.com"
}

locals {
  oidc_provider_arn = var.environment == "develop" ? aws_iam_openid_connect_provider.github[0].arn : data.aws_iam_openid_connect_provider.github[0].arn
}

resource "aws_iam_role" "github_deploy" {
  name = "${var.project_name}-${var.environment}-github-deploy"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Principal = {
        Federated = local.oidc_provider_arn
      }
      Action = "sts:AssumeRoleWithWebIdentity"
      Condition = {
        StringEquals = {
          "token.actions.githubusercontent.com:aud" = "sts.amazonaws.com"
        }
        StringLike = {
          "token.actions.githubusercontent.com:sub" = "repo:${var.github_repository}:ref:refs/heads/${var.environment == "prod" ? "main" : "develop"}"
        }
      }
    }]
  })
}

resource "aws_iam_role_policy" "github_deploy" {
  name = "${var.project_name}-${var.environment}-github-deploy-policy"
  role = aws_iam_role.github_deploy.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Sid    = "TerraformState"
        Effect = "Allow"
        Action = [
          "s3:GetObject",
          "s3:PutObject",
          "s3:DeleteObject",
          "s3:ListBucket"
        ]
        Resource = [
          "arn:aws:s3:::paulbulman-terraform-state",
          "arn:aws:s3:::paulbulman-terraform-state/parking/${var.environment}/*"
        ]
      },
      {
        Sid    = "Lambda"
        Effect = "Allow"
        Action = [
          "lambda:CreateFunction",
          "lambda:GetFunction",
          "lambda:GetFunctionConfiguration",
          "lambda:UpdateFunctionCode",
          "lambda:UpdateFunctionConfiguration",
          "lambda:DeleteFunction",
          "lambda:ListVersionsByFunction",
          "lambda:GetPolicy",
          "lambda:AddPermission",
          "lambda:RemovePermission",
          "lambda:GetFunctionCodeSigningConfig",
          "lambda:TagResource",
          "lambda:ListTags"
        ]
        Resource = [
          "arn:aws:lambda:${var.aws_region}:${data.aws_caller_identity.current.account_id}:function:${var.project_name}-${var.environment}-api",
          "arn:aws:lambda:${var.aws_region}:${data.aws_caller_identity.current.account_id}:function:${var.project_name}-${var.environment}-service",
          "arn:aws:lambda:${var.aws_region}:${data.aws_caller_identity.current.account_id}:function:${var.project_name}-${var.environment}-trigger",
          "arn:aws:lambda:${var.aws_region}:${data.aws_caller_identity.current.account_id}:function:${var.project_name}-${var.environment}-slack"
        ]
      },
      {
        Sid    = "ApiGateway"
        Effect = "Allow"
        Action = [
          "apigateway:GET",
          "apigateway:POST",
          "apigateway:PATCH",
          "apigateway:PUT",
          "apigateway:DELETE",
          "apigateway:TagResource",
          "apigateway:UntagResource"
        ]
        Resource = [
          "arn:aws:apigateway:${var.aws_region}::/apis",
          "arn:aws:apigateway:${var.aws_region}::/apis/*",
          "arn:aws:apigateway:${var.aws_region}::/tags/*"
        ]
      },
      {
        Sid    = "Cognito"
        Effect = "Allow"
        Action = [
          "cognito-idp:DescribeUserPool",
          "cognito-idp:UpdateUserPool",
          "cognito-idp:DeleteUserPool",
          "cognito-idp:CreateUserPoolClient",
          "cognito-idp:DescribeUserPoolClient",
          "cognito-idp:UpdateUserPoolClient",
          "cognito-idp:DeleteUserPoolClient",
          "cognito-idp:ListUserPoolClients",
          "cognito-idp:CreateGroup",
          "cognito-idp:GetGroup",
          "cognito-idp:DeleteGroup",
          "cognito-idp:ListGroups",
          "cognito-idp:GetUserPoolMfaConfig",
          "cognito-idp:TagResource",
          "cognito-idp:UntagResource",
          "cognito-idp:ListTagsForResource"
        ]
        Resource = "arn:aws:cognito-idp:${var.aws_region}:${data.aws_caller_identity.current.account_id}:userpool/*"
      },
      {
        Sid    = "CognitoCreate"
        Effect = "Allow"
        Action = [
          "cognito-idp:CreateUserPool",
          "cognito-idp:ListUserPools"
        ]
        Resource = "*"
      },
      {
        Sid    = "IAM"
        Effect = "Allow"
        Action = [
          "iam:GetRole",
          "iam:CreateRole",
          "iam:DeleteRole",
          "iam:PassRole",
          "iam:GetRolePolicy",
          "iam:PutRolePolicy",
          "iam:DeleteRolePolicy",
          "iam:ListRolePolicies",
          "iam:ListAttachedRolePolicies",
          "iam:ListInstanceProfilesForRole",
          "iam:ListRoleTags",
          "iam:TagRole",
          "iam:UntagRole",
          "iam:UpdateAssumeRolePolicy"
        ]
        Resource = [
          "arn:aws:iam::${data.aws_caller_identity.current.account_id}:role/${var.project_name}-${var.environment}-*"
        ]
      },
      {
        Sid    = "CloudWatchLogs"
        Effect = "Allow"
        Action = [
          "logs:CreateLogGroup",
          "logs:DeleteLogGroup",
          "logs:PutRetentionPolicy",
          "logs:ListTagsForResource",
          "logs:TagResource"
        ]
        Resource = [
          "arn:aws:logs:${var.aws_region}:${data.aws_caller_identity.current.account_id}:log-group:/aws/lambda/${var.project_name}-${var.environment}-api",
          "arn:aws:logs:${var.aws_region}:${data.aws_caller_identity.current.account_id}:log-group:/aws/lambda/${var.project_name}-${var.environment}-api:*",
          "arn:aws:logs:${var.aws_region}:${data.aws_caller_identity.current.account_id}:log-group:/aws/lambda/${var.project_name}-${var.environment}-service",
          "arn:aws:logs:${var.aws_region}:${data.aws_caller_identity.current.account_id}:log-group:/aws/lambda/${var.project_name}-${var.environment}-service:*",
          "arn:aws:logs:${var.aws_region}:${data.aws_caller_identity.current.account_id}:log-group:/aws/lambda/${var.project_name}-${var.environment}-trigger",
          "arn:aws:logs:${var.aws_region}:${data.aws_caller_identity.current.account_id}:log-group:/aws/lambda/${var.project_name}-${var.environment}-trigger:*",
          "arn:aws:logs:${var.aws_region}:${data.aws_caller_identity.current.account_id}:log-group:/aws/lambda/${var.project_name}-${var.environment}-slack",
          "arn:aws:logs:${var.aws_region}:${data.aws_caller_identity.current.account_id}:log-group:/aws/lambda/${var.project_name}-${var.environment}-slack:*",
          "arn:aws:logs:${var.aws_region}:${data.aws_caller_identity.current.account_id}:log-group:/aws/apigateway/${var.project_name}-${var.environment}",
          "arn:aws:logs:${var.aws_region}:${data.aws_caller_identity.current.account_id}:log-group:/aws/apigateway/${var.project_name}-${var.environment}:*"
        ]
      },
      {
        Sid    = "CloudWatchLogsList"
        Effect = "Allow"
        Action = [
          "logs:DescribeLogGroups"
        ]
        Resource = "arn:aws:logs:${var.aws_region}:${data.aws_caller_identity.current.account_id}:log-group:*"
      },
      {
        Sid    = "CloudWatchLogDelivery"
        Effect = "Allow"
        Action = [
          "logs:CreateLogDelivery",
          "logs:GetLogDelivery",
          "logs:UpdateLogDelivery",
          "logs:DeleteLogDelivery",
          "logs:ListLogDeliveries",
          "logs:PutResourcePolicy",
          "logs:DescribeResourcePolicies"
        ]
        Resource = "*"
      },
      {
        Sid    = "OIDCProvider"
        Effect = "Allow"
        Action = [
          "iam:GetOpenIDConnectProvider",
          "iam:ListOpenIDConnectProviders"
        ]
        Resource = "*"
      },
      {
        Sid    = "DynamoDB"
        Effect = "Allow"
        Action = [
          "dynamodb:CreateTable",
          "dynamodb:DeleteTable",
          "dynamodb:DescribeTable",
          "dynamodb:UpdateTable",
          "dynamodb:DescribeContinuousBackups",
          "dynamodb:UpdateContinuousBackups",
          "dynamodb:DescribeTimeToLive",
          "dynamodb:UpdateTimeToLive",
          "dynamodb:ListTagsOfResource",
          "dynamodb:TagResource",
          "dynamodb:UntagResource"
        ]
        Resource = "arn:aws:dynamodb:${var.aws_region}:${data.aws_caller_identity.current.account_id}:table/${var.project_name}-${var.environment}"
      },
      {
        Sid    = "Backup"
        Effect = "Allow"
        Action = [
          "backup:CreateBackupVault",
          "backup:DeleteBackupVault",
          "backup:DescribeBackupVault",
          "backup:ListTags",
          "backup:TagResource",
          "backup:UntagResource",
          "backup:CreateBackupPlan",
          "backup:DeleteBackupPlan",
          "backup:GetBackupPlan",
          "backup:UpdateBackupPlan",
          "backup:CreateBackupSelection",
          "backup:DeleteBackupSelection",
          "backup:GetBackupSelection"
        ]
        Resource = [
          "arn:aws:backup:${var.aws_region}:${data.aws_caller_identity.current.account_id}:backup-vault:${var.project_name}-${var.environment}",
          "arn:aws:backup:${var.aws_region}:${data.aws_caller_identity.current.account_id}:backup-plan:*"
        ]
      },
      {
        Sid      = "BackupStorage"
        Effect   = "Allow"
        Action   = "backup-storage:MountCapsule"
        Resource = "*"
      },
      {
        Sid    = "BackupVaultEncryption"
        Effect = "Allow"
        Action = [
          "kms:CreateGrant",
          "kms:GenerateDataKey",
          "kms:Decrypt",
          "kms:RetireGrant",
          "kms:DescribeKey"
        ]
        Resource = "arn:aws:kms:${var.aws_region}:${data.aws_caller_identity.current.account_id}:key/*"
      },
      {
        Sid    = "SNS"
        Effect = "Allow"
        Action = [
          "sns:CreateTopic",
          "sns:DeleteTopic",
          "sns:GetTopicAttributes",
          "sns:SetTopicAttributes",
          "sns:TagResource",
          "sns:UntagResource",
          "sns:ListTagsForResource",
          "sns:Subscribe",
          "sns:Unsubscribe",
          "sns:GetSubscriptionAttributes",
          "sns:ListSubscriptionsByTopic"
        ]
        Resource = "arn:aws:sns:${var.aws_region}:${data.aws_caller_identity.current.account_id}:${var.project_name}-${var.environment}"
      },
      {
        Sid    = "EventBridgeLegacy"
        Effect = "Allow"
        Action = [
          "events:PutRule",
          "events:DeleteRule",
          "events:DescribeRule",
          "events:PutTargets",
          "events:RemoveTargets",
          "events:ListTargetsByRule",
          "events:ListTagsForResource",
          "events:TagResource",
          "events:UntagResource"
        ]
        Resource = "arn:aws:events:${var.aws_region}:${data.aws_caller_identity.current.account_id}:rule/${var.project_name}-${var.environment}-*"
      },
      {
        Sid    = "Scheduler"
        Effect = "Allow"
        Action = [
          "scheduler:CreateSchedule",
          "scheduler:GetSchedule",
          "scheduler:UpdateSchedule",
          "scheduler:DeleteSchedule",
          "scheduler:CreateScheduleGroup",
          "scheduler:GetScheduleGroup",
          "scheduler:DeleteScheduleGroup",
          "scheduler:ListTagsForResource",
          "scheduler:TagResource",
          "scheduler:UntagResource"
        ]
        Resource = [
          "arn:aws:scheduler:${var.aws_region}:${data.aws_caller_identity.current.account_id}:schedule/${var.project_name}-${var.environment}/*",
          "arn:aws:scheduler:${var.aws_region}:${data.aws_caller_identity.current.account_id}:schedule-group/${var.project_name}-${var.environment}"
        ]
      },
      {
        Sid    = "SES"
        Effect = "Allow"
        Action = [
          "ses:DescribeActiveReceiptRuleSet",
          "ses:DescribeReceiptRule",
          "ses:DescribeReceiptRuleSet",
          "ses:CreateReceiptRule",
          "ses:UpdateReceiptRule",
          "ses:DeleteReceiptRule",
          "ses:SetActiveReceiptRuleSet",
          "ses:DescribeConfigurationSet",
          "ses:CreateConfigurationSet",
          "ses:DeleteConfigurationSet",
          "ses:GetIdentityVerificationAttributes",
          "ses:GetIdentityNotificationAttributes",
          "ses:GetIdentityDkimAttributes",
          "ses:GetIdentityMailFromDomainAttributes",
          "ses:VerifyDomainIdentity",
          "ses:DeleteIdentity",
          "ses:VerifyEmailIdentity",
          "ses:ListReceiptRuleSets"
        ]
        Resource = "*"
      },
      {
        Sid    = "SNSEmailTopic"
        Effect = "Allow"
        Action = [
          "sns:CreateTopic",
          "sns:DeleteTopic",
          "sns:GetTopicAttributes",
          "sns:SetTopicAttributes",
          "sns:TagResource",
          "sns:UntagResource",
          "sns:ListTagsForResource",
          "sns:Subscribe",
          "sns:Unsubscribe",
          "sns:GetSubscriptionAttributes",
          "sns:ListSubscriptionsByTopic"
        ]
        Resource = "arn:aws:sns:${var.ses_region}:${data.aws_caller_identity.current.account_id}:${var.project_name}-email"
      },
    ]
  })
}
