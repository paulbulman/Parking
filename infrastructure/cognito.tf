resource "aws_cognito_user_pool" "pool" {
  name = "${var.project_name}-${var.environment}"

  username_attributes      = ["email"]
  auto_verified_attributes = ["email"]
  sms_authentication_message = "Your authentication code is {####}. "

  admin_create_user_config {
    allow_admin_create_user_only = true

    invite_message_template {
      email_subject = "Your new parking rota account"
      email_message = "An new parking rota account has been created for you. If you were not expecting this, please email help@${var.ses_domain}<br /> <br /> To activate your new account, you need to log in within the next 7 days.<br /> <br /> You can log in at ${var.cognito_invite_url} using the username and temporary password below:<br /> {username}<br /> {####}<br /> <br /> When you first log in, you will need to set a new password. This must be at least 10 characters long, and not in the database of known-compromised passwords at https://haveibeenpwned.com/Passwords.<br /> <br /> A good way of generating a unique strong password is to use a password manager such as KeePass, 1Password or Bitwarden.<br /> <br /> If you have any questions, or need any help, please email help@${var.ses_domain}"
      sms_message   = "Your username is {username} and temporary password is {####}"
    }
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
