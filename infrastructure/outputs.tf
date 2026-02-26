output "api_endpoint" {
  value = aws_apigatewayv2_stage.default.invoke_url
}

output "api_function_name" {
  value = aws_lambda_function.api.function_name
}

output "service_function_name" {
  value = aws_lambda_function.service.function_name
}

output "slack_function_name" {
  value = aws_lambda_function.slack.function_name
}

output "user_pool_id" {
  value = aws_cognito_user_pool.pool.id
}

output "user_pool_client_id" {
  value = aws_cognito_user_pool_client.client.id
}

output "dynamodb_table_name" {
  value = aws_dynamodb_table.main.name
}

output "github_deploy_role_arn" {
  value = aws_iam_role.github_deploy.arn
}
