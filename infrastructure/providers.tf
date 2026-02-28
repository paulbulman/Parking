terraform {
  required_version = ">= 1.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.0"
    }
  }
}

provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = var.project_name
      Environment = var.environment
    }
  }
}

provider "aws" {
  alias  = "ses"
  region = var.ses_region

  default_tags {
    tags = {
      Project     = var.project_name
      Environment = var.environment
    }
  }
}
