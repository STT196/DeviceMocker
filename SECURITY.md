# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.0.x   | :white_check_mark: |

## Reporting a Vulnerability

We take the security of DeviceMocker seriously. If you believe you have found a security vulnerability, please report it to us as described below.

### How to Report

**Please do NOT report security vulnerabilities through public GitHub issues.**

Instead, please report them via:

1. **GitHub Security Advisories** (preferred)
   - Go to https://github.com/x1n-Q/DeviceMocker/security/advisories
   - Click "Report a vulnerability"
   - Fill out the form with details

2. **Email** (alternative)
   - Send an email to the project maintainer
   - Use subject line: `[SECURITY] DeviceMocker - <brief description>`

### What to Include

Please include the following information in your report:

- **Type of vulnerability** (e.g., buffer overflow, SQL injection, XSS)
- **Affected component** (e.g., SerialOutputService, KeyboardOutputService)
- **Steps to reproduce** with detailed instructions
- **Proof of concept** or exploit code (if available)
- **Potential impact** - what could an attacker achieve?
- **Suggested fix** (if you have one)

### Response Timeline

- **Acknowledgment**: Within 48 hours of receipt
- **Initial assessment**: Within 7 days
- **Resolution timeline**: Varies based on severity
  - Critical: 7 days
  - High: 14 days
  - Medium: 30 days
  - Low: 90 days

### What to Expect

1. We will acknowledge receipt of your report
2. We will investigate and validate the vulnerability
3. We will work on a fix and coordinate release
4. We will credit you (unless you prefer to remain anonymous)
5. We will publish a security advisory

### Security Considerations

DeviceMocker uses several Windows APIs that require careful handling:

- **Windows SendInput API** - Used for keyboard simulation
- **Serial Port Communication** - Direct hardware access
- **Network Sockets** - TCP/UDP communication
- **HTTP Requests** - Webhook functionality

Please consider these when testing and reporting vulnerabilities.

### Responsible Disclosure

We ask that you:

- Give us reasonable time to fix the issue before public disclosure
- Avoid exploiting the vulnerability beyond what's necessary to demonstrate it
- Not use the vulnerability to access, modify, or delete data
- Make a good faith effort to avoid privacy violations and service disruption

Thank you for helping keep DeviceMocker and its users safe!
