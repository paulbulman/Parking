variable "aws_region" {
  type    = string
  default = "eu-west-2"
}

variable "environment" {
  type = string
}

variable "project_name" {
  type    = string
  default = "parking"
}

variable "github_repository" {
  type        = string
  description = "GitHub repository in format owner/repo"
}

variable "cors_allowed_origins" {
  type        = string
  description = "Comma-separated list of allowed CORS origins"
  default     = ""
}

variable "ses_from_address" {
  type        = string
  description = "Email address for sending emails from (must be verified in SES)"
}

variable "ses_region" {
  type        = string
  description = "AWS region where the SES identity is configured"
  default     = "eu-west-1"
}

variable "ses_domain" {
  type        = string
  description = "Domain for SES identity"
}

variable "sns_email_subscription" {
  type        = string
  description = "Email address for SNS topic subscription"
}

variable "slack_webhook_url" {
  type        = string
  description = "Slack webhook URL for notifications"
  sensitive   = true
  default     = ""
}

variable "api_lambda_package_path" {
  type        = string
  description = "Local path to the API Lambda zip"
  default     = "api-lambda.zip"
}

variable "service_lambda_package_path" {
  type        = string
  description = "Local path to the Service Lambda zip"
  default     = "service-lambda.zip"
}

variable "cognito_invite_url" {
  type        = string
  description = "Website URL for sign-in link in Cognito invite emails"
}
