# Minutely rule - invokes Service Lambda to process triggers

resource "aws_cloudwatch_event_rule" "minutely" {
  name                = "${var.project_name}-${var.environment}-minutely"
  schedule_expression = "rate(1 minute)"
}

resource "aws_cloudwatch_event_target" "minutely_service" {
  rule = aws_cloudwatch_event_rule.minutely.name
  arn  = aws_lambda_function.service.arn
}

resource "aws_lambda_permission" "eventbridge_minutely" {
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.service.function_name
  principal     = "events.amazonaws.com"
  source_arn    = aws_cloudwatch_event_rule.minutely.arn
}

# Hourly and TwoMinutesPast rules - add triggers via Trigger Lambda

resource "aws_cloudwatch_event_rule" "hourly" {
  name                = "${var.project_name}-${var.environment}-hourly"
  schedule_expression = "cron(0 * * * ? *)"
}

resource "aws_cloudwatch_event_target" "hourly_trigger" {
  rule = aws_cloudwatch_event_rule.hourly.name
  arn  = aws_lambda_function.trigger.arn
}

resource "aws_lambda_permission" "eventbridge_hourly" {
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.trigger.function_name
  principal     = "events.amazonaws.com"
  source_arn    = aws_cloudwatch_event_rule.hourly.arn
}

resource "aws_cloudwatch_event_rule" "two_minutes_past" {
  name                = "${var.project_name}-${var.environment}-two-minutes-past"
  schedule_expression = "cron(2 10,11,23,0 * * ? *)"
}

resource "aws_cloudwatch_event_target" "two_minutes_past_trigger" {
  rule = aws_cloudwatch_event_rule.two_minutes_past.name
  arn  = aws_lambda_function.trigger.arn
}

resource "aws_lambda_permission" "eventbridge_two_minutes_past" {
  action        = "lambda:InvokeFunction"
  function_name = aws_lambda_function.trigger.function_name
  principal     = "events.amazonaws.com"
  source_arn    = aws_cloudwatch_event_rule.two_minutes_past.arn
}
