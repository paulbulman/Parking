resource "aws_lambda_function" "api" {
  function_name = "${var.project_name}-${var.environment}-api"
  role          = aws_iam_role.api_lambda.arn
  handler       = "Parking.Api::Parking.Api.LambdaEntryPoint::FunctionHandlerAsync"
  runtime       = "dotnet10"
  architectures = ["x86_64"]
  memory_size   = 10240
  timeout       = 30

  filename         = var.api_lambda_package_path
  source_code_hash = filebase64sha256(var.api_lambda_package_path)

  environment {
    variables = {
      TABLE_NAME        = aws_dynamodb_table.main.name
      USER_POOL_ID      = aws_cognito_user_pool.pool.id
      TOPIC_NAME        = aws_sns_topic.main.arn
      FROM_EMAIL_ADDRESS = var.ses_from_address
      SMTP_CONFIG_SET   = var.project_name
      CORS_ORIGIN       = var.cors_allowed_origins
    }
  }

  depends_on = [aws_cloudwatch_log_group.api_lambda]
}

resource "aws_lambda_function" "service" {
  function_name = "${var.project_name}-${var.environment}-service"
  role          = aws_iam_role.service_lambda.arn
  handler       = "Parking.Service::Parking.Service.LambdaEntryPoint::RunTasks"
  runtime       = "dotnet10"
  architectures = ["x86_64"]
  memory_size   = 10240
  timeout       = 30

  filename         = var.service_lambda_package_path
  source_code_hash = filebase64sha256(var.service_lambda_package_path)

  environment {
    variables = {
      TABLE_NAME        = aws_dynamodb_table.main.name
      USER_POOL_ID      = aws_cognito_user_pool.pool.id
      TOPIC_NAME        = aws_sns_topic.main.arn
      FROM_EMAIL_ADDRESS = var.ses_from_address
      SMTP_CONFIG_SET   = var.project_name
    }
  }

  depends_on = [aws_cloudwatch_log_group.service_lambda]
}

resource "aws_lambda_function" "trigger" {
  function_name = "${var.project_name}-${var.environment}-trigger"
  role          = aws_iam_role.trigger_lambda.arn
  handler       = "Parking.Service::Parking.Service.LambdaEntryPoint::AddTrigger"
  runtime       = "dotnet10"
  architectures = ["x86_64"]
  memory_size   = 256
  timeout       = 10

  filename         = var.service_lambda_package_path
  source_code_hash = filebase64sha256(var.service_lambda_package_path)

  environment {
    variables = {
      TABLE_NAME = aws_dynamodb_table.main.name
    }
  }

  depends_on = [aws_cloudwatch_log_group.trigger_lambda]
}

data "archive_file" "cognito_email" {
  type        = "zip"
  source_dir  = "${path.module}/../cognito-email"
  output_path = "${path.module}/cognito-email.zip"
}

resource "aws_lambda_function" "cognito_email" {
  function_name = "${var.project_name}-${var.environment}-cognito-email"
  role          = aws_iam_role.cognito_email_lambda.arn
  handler       = "index.handler"
  runtime       = "nodejs22.x"
  architectures = ["x86_64"]
  memory_size   = 128
  timeout       = 5

  filename         = data.archive_file.cognito_email.output_path
  source_code_hash = data.archive_file.cognito_email.output_base64sha256

  environment {
    variables = {
      WEBSITE_URL = var.cognito_invite_url
      HELP_EMAIL  = "help@${var.ses_domain}"
      ENVIRONMENT = var.environment
    }
  }

  depends_on = [aws_cloudwatch_log_group.cognito_email_lambda]
}

resource "aws_lambda_permission" "cognito_email" {
  statement_id  = "AllowCognitoInvoke"
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.cognito_email.function_name
  principal     = "cognito-idp.amazonaws.com"
  source_arn    = aws_cognito_user_pool.pool.arn
}

data "archive_file" "slack" {
  type        = "zip"
  source_dir  = "${path.module}/slack-lambda"
  output_path = "${path.module}/slack-lambda.zip"
}

resource "aws_lambda_function" "slack" {
  function_name = "${var.project_name}-${var.environment}-slack"
  role          = aws_iam_role.slack_lambda.arn
  handler       = "lambda_function.lambda_handler"
  runtime       = "python3.13"
  architectures = ["x86_64"]
  memory_size   = 128
  timeout       = 10

  filename         = data.archive_file.slack.output_path
  source_code_hash = data.archive_file.slack.output_base64sha256

  environment {
    variables = {
      SLACK_WEBHOOK_URL = var.slack_webhook_url
    }
  }

  depends_on = [aws_cloudwatch_log_group.slack_lambda]
}
