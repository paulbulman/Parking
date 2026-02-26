resource "aws_dynamodb_table" "main" {
  name         = "${var.project_name}-${var.environment}"
  billing_mode = "PAY_PER_REQUEST"
  hash_key     = "PK"
  range_key    = "SK"

  attribute {
    name = "PK"
    type = "S"
  }

  attribute {
    name = "SK"
    type = "S"
  }

  global_secondary_index {
    name = "SK-PK-index"
    key_schema {
      attribute_name = "SK"
      key_type       = "HASH"
    }
    key_schema {
      attribute_name = "PK"
      key_type       = "RANGE"
    }
    projection_type = "ALL"
  }

  point_in_time_recovery {
    enabled = true
  }

  deletion_protection_enabled = true

  tags = {
    Name        = "${var.project_name}-${var.environment}"
    Environment = var.environment
  }
}
