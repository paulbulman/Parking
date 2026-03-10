const { WEBSITE_URL, HELP_EMAIL, ENVIRONMENT } = process.env;

const appName =
  ENVIRONMENT === "prod" ? "Parking Rota" : "Parking Rota (test system)";

const teal = "#0d7377";
const tealDark = "#0a5c5f";
const tealSubtle = "#e6f3f3";
const textDark = "#1a1d21";
const textMuted = "#5a5f66";
const textLight = "#8a8f96";
const bgPage = "#f3f4f6";

const bodyFont =
  "'Segoe UI',Roboto,-apple-system,BlinkMacSystemFont,Helvetica,Arial,sans-serif";
const monoFont = "'Cascadia Code','Fira Code','SF Mono',Consolas,monospace";

export const handler = async (event) => {
  switch (event.triggerSource) {
    case "CustomMessage_AdminCreateUser":
      event.response.emailSubject = `Your new ${appName} account`;
      event.response.emailMessage = inviteEmail(
        event.request.usernameParameter,
        event.request.codeParameter
      );
      break;

    case "CustomMessage_ForgotPassword":
      event.response.emailSubject = `Reset your ${appName} password`;
      event.response.emailMessage = forgotPasswordEmail(
        event.request.codeParameter
      );
      break;
  }

  return event;
};

const inviteEmail = (username, temporaryPassword) =>
  layout(`
  <p style="margin:0 0 18px;font-family:${bodyFont};font-size:15px;line-height:1.6;color:${textDark};">
    A new ${appName} account has been created for you. If you were not expecting this, please email
    <a href="mailto:${HELP_EMAIL}" style="color:${teal};text-decoration:none;">${HELP_EMAIL}</a>.
  </p>
  <p style="margin:0 0 22px;font-family:${bodyFont};font-size:15px;line-height:1.6;color:${textDark};">
    To activate your account, you need to log in within the next <strong>7 days</strong>.
  </p>
  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:0 0 24px;">
    <tr>
      <td style="padding:18px 22px;background:${tealSubtle};border-left:3px solid ${teal};border-radius:4px;">
        <p style="margin:0 0 2px;font-family:${bodyFont};font-size:11px;color:${textLight};text-transform:uppercase;letter-spacing:0.8px;font-weight:600;">
          Username
        </p>
        <p style="margin:0 0 16px;font-family:${bodyFont};font-size:15px;font-weight:600;color:${textDark};">
          ${username}
        </p>
        <p style="margin:0 0 2px;font-family:${bodyFont};font-size:11px;color:${textLight};text-transform:uppercase;letter-spacing:0.8px;font-weight:600;">
          Temporary password
        </p>
        <p style="margin:0;font-family:${monoFont};font-size:16px;font-weight:600;color:${textDark};letter-spacing:0.5px;">
          ${temporaryPassword}
        </p>
      </td>
    </tr>
  </table>
  <table role="presentation" cellpadding="0" cellspacing="0" style="margin:0 0 24px;">
    <tr>
      <td style="padding:12px 28px;background:${teal};border-radius:6px;">
        <a href="${WEBSITE_URL}" style="font-family:${bodyFont};color:#ffffff;text-decoration:none;font-weight:600;font-size:14px;">
          Sign in &rarr;
        </a>
      </td>
    </tr>
  </table>
  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:0;border-top:1px solid #e5e7eb;">
    <tr>
      <td style="padding:18px 0 0;">
        <p style="margin:0 0 6px;font-family:${bodyFont};font-size:13px;line-height:1.6;color:${textMuted};">
          When you first log in, you will need to set a new password. This must be at least 10 characters long,
          and not in the database of known-compromised passwords at
          <a href="https://haveibeenpwned.com/Passwords" style="color:${teal};text-decoration:none;">haveibeenpwned.com</a>.
        </p>
        <p style="margin:0;font-family:${bodyFont};font-size:13px;line-height:1.6;color:${textMuted};">
          A good way of generating a unique strong password is to use a password manager such as KeePass, 1Password or Bitwarden.
        </p>
      </td>
    </tr>
  </table>
`);

const forgotPasswordEmail = (code) =>
  layout(`
  <p style="margin:0 0 22px;font-family:${bodyFont};font-size:15px;line-height:1.6;color:${textDark};">
    Enter the following code to reset your password.
  </p>
  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="margin:0 0 22px;">
    <tr>
      <td style="padding:22px;background:${tealSubtle};border-radius:6px;text-align:center;">
        <p style="margin:0;font-family:${monoFont};font-size:30px;font-weight:700;color:${tealDark};letter-spacing:8px;">
          ${code}
        </p>
      </td>
    </tr>
  </table>
  <p style="margin:0;font-family:${bodyFont};font-size:13px;color:${textLight};">
    If you didn't request this, you can safely ignore this email.
  </p>
`);

const layout = (content) => `
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width,initial-scale=1">
  <title>${appName}</title>
</head>
<body style="margin:0;padding:0;background:${bgPage};font-family:${bodyFont};font-size:15px;line-height:1.6;color:${textDark};">
  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:${bgPage};">
    <tr>
      <td style="padding:48px 20px 32px;" align="center">
        <table role="presentation" width="480" cellpadding="0" cellspacing="0" style="max-width:480px;width:100%;">
          <tr>
            <td style="padding:0 0 28px;" align="center">
              <p style="margin:0;font-family:${bodyFont};font-size:20px;font-weight:700;color:${tealDark};letter-spacing:0.2px;">
                ${appName}
              </p>
            </td>
          </tr>
          <tr>
            <td style="padding:30px 34px;background:#ffffff;border-top:3px solid ${teal};border-radius:8px;box-shadow:0 1px 3px rgba(0,0,0,0.06);">
              ${content}
            </td>
          </tr>
          <tr>
            <td style="padding:24px 0 0;" align="center">
              <p style="margin:0;font-family:${bodyFont};font-size:11px;color:${textLight};letter-spacing:0.3px;">
                ${appName}
              </p>
            </td>
          </tr>
        </table>
      </td>
    </tr>
  </table>
</body>
</html>
`;
