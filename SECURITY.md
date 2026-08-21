# Security Policy

## Supported versions

ServicePilot is a portfolio project and is not currently operated as a hosted
service. Security fixes are applied to the latest `main` branch only.

## Reporting a vulnerability

Please do not disclose suspected vulnerabilities in a public issue, discussion
or pull request.

Use [GitHub private vulnerability reporting](https://github.com/Bekican/servicepilot/security/advisories/new)
and include:

- the affected component and commit;
- reproducible steps or a minimal proof of concept;
- expected and observed impact;
- any suggested mitigation, if available.

Do not access data that is not yours, degrade a third-party system, perform
social engineering or retain sensitive material while researching a report.

You can expect an initial acknowledgement within seven days. Timelines for
validation and remediation depend on severity and project availability. Please
allow a reasonable remediation window before public disclosure.

## Secrets and local configuration

Values in `.env.example`, `appsettings.Development.json`, demo scripts and test
fixtures are intentionally disposable local examples. They are not production
credentials. A real deployment must inject unique secrets through its runtime
secret store and must never commit them to this repository.
