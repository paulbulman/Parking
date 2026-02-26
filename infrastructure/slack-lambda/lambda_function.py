import json
import os
import urllib.request


def lambda_handler(event, context):
    webhook_url = os.environ["SLACK_WEBHOOK_URL"]

    for record in event["Records"]:
        message = record["Sns"]["Message"]

        payload = json.dumps({"text": message}).encode("utf-8")

        request = urllib.request.Request(
            webhook_url,
            data=payload,
            headers={"Content-Type": "application/json"},
        )

        urllib.request.urlopen(request)
