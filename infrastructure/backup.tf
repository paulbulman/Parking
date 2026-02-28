resource "aws_backup_vault" "main" {
  name = "${var.project_name}-${var.environment}"
}

resource "aws_backup_plan" "main" {
  name = "${var.project_name}-${var.environment}"

  rule {
    rule_name         = "monthly"
    target_vault_name = aws_backup_vault.main.name
    schedule          = "cron(0 3 1 * ? *)"

    lifecycle {
      delete_after = 365
    }
  }

}

resource "aws_iam_role" "backup" {
  name = "${var.project_name}-${var.environment}-backup-role"

  assume_role_policy = jsonencode({
    Version = "2012-10-17"
    Statement = [{
      Action = "sts:AssumeRole"
      Effect = "Allow"
      Principal = {
        Service = "backup.amazonaws.com"
      }
    }]
  })
}

resource "aws_iam_role_policy" "backup" {
  name = "${var.project_name}-${var.environment}-backup-policy"
  role = aws_iam_role.backup.id

  policy = jsonencode({
    Version = "2012-10-17"
    Statement = [
      {
        Effect = "Allow"
        Action = [
          "dynamodb:CreateBackup",
          "dynamodb:DescribeTable"
        ]
        Resource = aws_dynamodb_table.main.arn
      },
      {
        Effect = "Allow"
        Action = [
          "dynamodb:DeleteBackup",
          "dynamodb:DescribeBackup"
        ]
        Resource = "${aws_dynamodb_table.main.arn}/backup/*"
      },
      {
        Effect   = "Allow"
        Action   = "backup:TagResource"
        Resource = aws_backup_vault.main.arn
      }
    ]
  })
}

resource "aws_backup_selection" "main" {
  name         = "${var.project_name}-${var.environment}-dynamodb"
  plan_id      = aws_backup_plan.main.id
  iam_role_arn = aws_iam_role.backup.arn

  resources = [
    aws_dynamodb_table.main.arn
  ]
}
