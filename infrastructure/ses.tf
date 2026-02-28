# SES resources are shared across environments (eu-west-1).
# Only create when environment == "develop" to avoid duplicates.

resource "aws_ses_domain_identity" "main" {
  count    = var.environment == "develop" ? 1 : 0
  provider = aws.ses
  domain   = var.ses_domain
}

resource "aws_ses_email_identity" "from" {
  provider = aws.ses
  email    = var.ses_from_address
}

resource "aws_ses_configuration_set" "main" {
  count    = var.environment == "develop" ? 1 : 0
  provider = aws.ses
  name     = var.project_name
}

resource "aws_ses_active_receipt_rule_set" "main" {
  count         = var.environment == "develop" ? 1 : 0
  provider      = aws.ses
  rule_set_name = "default"
}

resource "aws_ses_receipt_rule" "forward" {
  count         = var.environment == "develop" ? 1 : 0
  provider      = aws.ses
  name          = "forward-incoming-via-sns"
  rule_set_name = "default"
  enabled       = true
  scan_enabled  = true

  sns_action {
    topic_arn = aws_sns_topic.email[0].arn
    position  = 1
  }

  depends_on = [aws_ses_active_receipt_rule_set.main]
}

resource "aws_sns_topic" "email" {
  count        = var.environment == "develop" ? 1 : 0
  provider     = aws.ses
  name         = "${var.project_name}-email"
  display_name = "Parking email"
}
