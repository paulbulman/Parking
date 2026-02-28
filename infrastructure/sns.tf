resource "aws_sns_topic" "main" {
  name = "${var.project_name}-${var.environment}"
}

resource "aws_sns_topic_subscription" "email" {
  topic_arn = aws_sns_topic.main.arn
  protocol  = "email"
  endpoint  = var.sns_email_subscription
}

resource "aws_sns_topic_subscription" "slack" {
  topic_arn = aws_sns_topic.main.arn
  protocol  = "lambda"
  endpoint  = aws_lambda_function.slack.arn
}

resource "aws_lambda_permission" "sns_slack" {
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.slack.function_name
  principal     = "sns.amazonaws.com"
  source_arn    = aws_sns_topic.main.arn
}
