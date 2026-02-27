resource "aws_scheduler_schedule_group" "main" {
  name = "${var.project_name}-${var.environment}"
}

# IAM role for EventBridge Scheduler to invoke Lambda

resource "aws_iam_role" "scheduler" {
  name = "${var.project_name}-${var.environment}-scheduler-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "scheduler.amazonaws.com"
      }
    }]
  })
}

resource "aws_iam_role_policy" "scheduler" {
  name = "${var.project_name}-${var.environment}-scheduler-policy"
  role = aws_iam_role.scheduler.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Effect = "Allow"
      Action = "lambda:InvokeFunction"
      Resource = [
        aws_lambda_function.service.arn,
        aws_lambda_function.trigger.arn
      ]
    }]
  })
}

# Minutely - invokes Service Lambda to process triggers

resource "aws_scheduler_schedule" "minutely" {
  name       = "${var.project_name}-${var.environment}-minutely"
  group_name = aws_scheduler_schedule_group.main.name

  schedule_expression = "rate(1 minute)"

  flexible_time_window {
    mode = "OFF"
  }

  target {
    arn      = aws_lambda_function.service.arn
    role_arn = aws_iam_role.scheduler.arn
  }
}

# Hourly - adds trigger via Trigger Lambda

resource "aws_scheduler_schedule" "hourly" {
  name       = "${var.project_name}-${var.environment}-hourly"
  group_name = aws_scheduler_schedule_group.main.name

  schedule_expression = "cron(0 * * * ? *)"

  flexible_time_window {
    mode = "OFF"
  }

  target {
    arn      = aws_lambda_function.trigger.arn
    role_arn = aws_iam_role.scheduler.arn
  }
}

# Two minutes past - adds trigger via Trigger Lambda

resource "aws_scheduler_schedule" "two_minutes_past" {
  name       = "${var.project_name}-${var.environment}-two-minutes-past"
  group_name = aws_scheduler_schedule_group.main.name

  schedule_expression = "cron(2 10,11,23,0 * * ? *)"

  flexible_time_window {
    mode = "OFF"
  }

  target {
    arn      = aws_lambda_function.trigger.arn
    role_arn = aws_iam_role.scheduler.arn
  }
}
