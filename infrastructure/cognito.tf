resource "aws_cognito_user_pool" "pool" {
  name = "${var.project_name}-${var.environment}"

  deletion_protection      = "ACTIVE"
  username_attributes      = ["email"]
  auto_verified_attributes = ["email"]
  sms_authentication_message = "Your authentication code is {####}. "

  admin_create_user_config {
    allow_admin_create_user_only = true
  }

  lambda_config {
    custom_message = aws_lambda_function.cognito_email.arn
  }

  password_policy {
    minimum_length                   = 10
    require_lowercase                = false
    require_numbers                  = false
    require_symbols                  = false
    require_uppercase                = false
    temporary_password_validity_days = 7
  }

  schema {
    name                = "family_name"
    attribute_data_type = "String"
    required            = true
    mutable             = true
  }

  schema {
    name                = "given_name"
    attribute_data_type = "String"
    required            = true
    mutable             = true
  }

  mfa_configuration = "OPTIONAL"

  software_token_mfa_configuration {
    enabled = true
  }

  account_recovery_setting {
    recovery_mechanism {
      name     = "verified_email"
      priority = 1
    }
  }

  email_configuration {
    email_sending_account  = "DEVELOPER"
    source_arn             = "arn:aws:ses:${var.ses_region}:${data.aws_caller_identity.current.account_id}:identity/${var.ses_domain}"
    from_email_address     = "help@${var.ses_domain}"
    reply_to_email_address = "help@${var.ses_domain}"
  }
}

resource "aws_cognito_user_group" "team_leader" {
  name         = "TeamLeader"
  user_pool_id = aws_cognito_user_pool.pool.id
  description  = "Edit reservations; update other users' requests"
  role_arn     = "arn:aws:iam::${data.aws_caller_identity.current.account_id}:role/cognitgo-group-placeholder"
}

resource "aws_cognito_user_group" "user_admin" {
  name         = "UserAdmin"
  user_pool_id = aws_cognito_user_pool.pool.id
  description  = "Manage user accounts"
  role_arn     = "arn:aws:iam::${data.aws_caller_identity.current.account_id}:role/cognitgo-group-placeholder"
}

resource "aws_cognito_user_pool_client" "client" {
  name         = "website"
  user_pool_id = aws_cognito_user_pool.pool.id

  prevent_user_existence_errors = "ENABLED"

  explicit_auth_flows = [
    "ALLOW_REFRESH_TOKEN_AUTH",
    "ALLOW_USER_SRP_AUTH",
  ]

  token_validity_units {
    access_token  = "minutes"
    id_token      = "minutes"
    refresh_token = "days"
  }
}
