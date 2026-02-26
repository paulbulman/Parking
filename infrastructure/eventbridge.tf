# Minutely rule - invokes Service Lambda directly

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

# Hourly and TwoMinutesPast rules - invoke API Lambda via API Gateway

resource "aws_iam_role" "eventbridge_api" {
  name = "${var.project_name}-${var.environment}-eventbridge-api-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "events.amazonaws.com"
      }
    }]
  })
}

resource "aws_iam_role_policy" "eventbridge_api" {
  name = "${var.project_name}-${var.environment}-eventbridge-api-policy"
  role = aws_iam_role.eventbridge_api.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect   = "Allow"
      Action   = "execute-api:Invoke"
      Resource = "${aws_apigatewayv2_api.api.execution_arn}/*/POST/triggers"
    }]
  })
}

resource "aws_cloudwatch_event_rule" "hourly" {
  name                = "${var.project_name}-${var.environment}-hourly"
  schedule_expression = "cron(0 * * * ? *)"
}

resource "aws_cloudwatch_event_target" "hourly_api" {
  rule     = aws_cloudwatch_event_rule.hourly.name
  arn      = "${aws_apigatewayv2_api.api.execution_arn}/$default/POST/triggers"
  role_arn = aws_iam_role.eventbridge_api.arn

  http_target {
    header_parameters = {
      "Content-Type" = "application/json"
    }
  }
}

resource "aws_cloudwatch_event_rule" "two_minutes_past" {
  name                = "${var.project_name}-${var.environment}-two-minutes-past"
  schedule_expression = "cron(2 10,11,23,0 * * ? *)"
}

resource "aws_cloudwatch_event_target" "two_minutes_past_api" {
  rule     = aws_cloudwatch_event_rule.two_minutes_past.name
  arn      = "${aws_apigatewayv2_api.api.execution_arn}/$default/POST/triggers"
  role_arn = aws_iam_role.eventbridge_api.arn

  http_target {
    header_parameters = {
      "Content-Type" = "application/json"
    }
  }
}
