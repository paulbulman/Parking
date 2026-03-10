resource "aws_cloudwatch_log_group" "api_lambda" {
  name              = "/aws/lambda/${var.project_name}-${var.environment}-api"
  retention_in_days = 14
}

resource "aws_cloudwatch_log_group" "service_lambda" {
  name              = "/aws/lambda/${var.project_name}-${var.environment}-service"
  retention_in_days = 14
}

resource "aws_cloudwatch_log_group" "trigger_lambda" {
  name              = "/aws/lambda/${var.project_name}-${var.environment}-trigger"
  retention_in_days = 14
}

resource "aws_cloudwatch_log_group" "cognito_email_lambda" {
  name              = "/aws/lambda/${var.project_name}-${var.environment}-cognito-email"
  retention_in_days = 14
}

resource "aws_cloudwatch_log_group" "slack_lambda" {
  name              = "/aws/lambda/${var.project_name}-${var.environment}-slack"
  retention_in_days = 14
}

resource "aws_cloudwatch_log_group" "api_gateway" {
  name              = "/aws/apigateway/${var.project_name}-${var.environment}"
  retention_in_days = 14
}
