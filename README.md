# Thomas

### Autonomous AI Software Reliability Engineer

**From production errors to reviewed pull requests.**

Thomas connects observability platforms, source-code repositories, and
AI providers to investigate software incidents, propose code changes,
validate fixes, and prepare pull requests for human review.

[Overview](#overview) · [How It Works](#how-it-works) ·
[Architecture](#architecture) · [Roadmap](#roadmap) ·
[Security](#security)

------------------------------------------------------------------------

![Status](https://img.shields.io/badge/status-early%20development-orange)
![AI](https://img.shields.io/badge/AI-agentic%20workflows-7B61FF)
![Workflow](https://img.shields.io/badge/workflow-error%20to%20PR-2563EB)


> **Project vision:** Turn a production error into an evidence-backed,
> tested pull request that a developer can review --- without handing an
> AI agent uncontrolled access to production.

------------------------------------------------------------------------

## Overview

Thomas is an AI-powered software reliability agent designed to help
engineering teams investigate application errors and reduce the time
between detecting an issue and proposing a fix.

It connects to observability tools such as **New Relic, Datadog, Splunk,
and Kibana**, retrieves relevant error context, investigates the
affected codebase, and uses configurable AI providers to propose a
remediation.

When a potential fix is ready, Thomas can work in an isolated
environment, run the available validation steps, and open a **GitHub
pull request** containing the proposed change, investigation summary,
and supporting evidence.

Thomas is envisioned as a multi-project platform: teams can connect
multiple repositories and observability sources, configure owners and
policies, and follow incidents and proposed remediations from a central
dashboard.

### The problem

Investigating production errors often requires engineers to move between
dashboards, logs, traces, source code, deployment history, and team
knowledge. Repeated incidents can consume time even when a similar issue
has already been investigated.

Thomas aims to bring this context together and automate the repetitive
parts of the workflow while keeping engineers in control of code
changes.

### Core principles

-   **Evidence before changes:** every proposed fix should be connected
    to the error context and investigation findings.
-   **Human review by default:** Thomas proposes changes through pull
    requests; it does not independently merge them.
-   **Project-aware investigation:** analysis should consider the
    relevant repository, service, history, and technical context.
-   **Provider flexibility:** observability integrations and AI
    providers should be replaceable.
-   **Continuous learning:** incident outcomes and approved resolutions
    can enrich the project's knowledge base.

------------------------------------------------------------------------

## How It Works

### 1. Detect

Thomas receives or retrieves error events from connected observability
platforms. It collects relevant context such as timestamps, stack
traces, affected services, error frequency, and available trace or
deployment information.

### 2. Correlate

Related events are grouped to reduce noise and identify recurring
patterns. Thomas can compare a new incident with previous incidents and
known resolutions.

### 3. Investigate

The investigation engine analyzes the available evidence and the
connected repository. It forms a hypothesis about the likely root cause
and identifies the code areas that may be involved.

### 4. Remediate

When the evidence is sufficient, Thomas creates an isolated working
branch and prepares a proposed code change. The change is not applied
directly to a production environment.

### 5. Validate

Thomas runs the validation steps available to the project, such as unit
tests, integration tests, static analysis, and build checks. Results and
limitations are included in the proposed pull request.

### 6. Explain and notify

Thomas opens a pull request with a human-readable explanation:

-   What error was detected?
-   What is the likely cause?
-   What evidence supports the diagnosis?
-   What was changed?
-   Why should the change address the issue?
-   Which tests were executed, and what were their results?
-   What risks or uncertainties remain?

The relevant code owners can then review the proposal and decide whether
to merge it.

------------------------------------------------------------------------

## Key Capabilities

> The capabilities below describe the product direction. Features are
> subject to implementation and validation; consult the project roadmap
> for development status.

  -----------------------------------------------------------------------
  Capability                          Description
  ----------------------------------- -----------------------------------
  Observability integrations          Connect to supported logging and
                                      monitoring platforms.

  Error correlation                   Group similar events and identify
                                      recurring incidents.

  AI-assisted investigation           Analyze error context and
                                      repository information to propose
                                      root causes.

  Repository intelligence             Retrieve relevant code,
                                      dependencies, history, and project
                                      documentation.

  Automated remediation               Prepare candidate fixes in isolated
                                      branches.

  Validation workflow                 Run configured tests and quality
                                      checks before opening a PR.

  Pull request generation             Create a PR with the proposed fix
                                      and investigation report.

  Owner notifications                 Notify responsible engineers
                                      through configured channels.

  Incident memory                     Retain structured incident history
                                      and resolution outcomes.

  Prioritization                      Help teams assess incidents using
                                      frequency, impact, severity, and
                                      recurrence.

  Multi-project dashboard             Manage connected projects,
                                      incidents, integrations, and
                                      remediation activity.
  -----------------------------------------------------------------------

------------------------------------------------------------------------

## Intelligence & Memory

A central part of Thomas is its ability to use previous incident
investigations as context for future ones.

The intended knowledge model combines structured operational data with
searchable technical knowledge.

### Operational memory

Stores incident-level information, including:

-   Error signatures and affected services.
-   Occurrence frequency and timestamps.
-   Related deployments and changes, when available.
-   Investigation hypotheses and confidence.
-   Proposed and accepted resolutions.
-   Test and validation outcomes.
-   Human review decisions.

### Knowledge base

Provides searchable context from sources such as:

-   Project documentation.
-   Architecture notes and runbooks.
-   Known issues and troubleshooting guides.
-   Previous incident reports.
-   Approved remediation patterns.

### Code intelligence

Connects incidents to the codebase using information such as:

-   Relevant files, classes, and dependencies.
-   Commit and pull request history.
-   Ownership metadata.
-   Test coverage and available validation commands.

The goal is not to let an AI model make decisions from memory alone.
Historical knowledge should be combined with current evidence, and the
system should make uncertainty visible.

------------------------------------------------------------------------

## Prioritization: Not Every Error Needs a Fix

Thomas is intended to help teams distinguish actionable incidents from
low-impact noise.

A configurable prioritization model may consider:

-   Error frequency and rate of increase.
-   Number of affected users or requests.
-   Service criticality.
-   Business and functional impact.
-   Severity and duration.
-   Recent deployments or configuration changes.
-   Availability of a workaround.
-   Previous occurrences and resolution history.

The result should support engineering decisions, not replace them. Teams
should be able to inspect the factors behind a priority recommendation
and adjust policies to their environment.

------------------------------------------------------------------------

## Architecture

Thomas is envisioned as a modular platform with clear boundaries between
integrations, event processing, agent execution, persistence, and
user-facing workflows.

### Proposed technology direction

The technology choices below are initial architectural preferences, not
a claim that every component has already been implemented.

  -----------------------------------------------------------------------
  Area                                Proposed technology
  ----------------------------------- -----------------------------------
  Backend and APIs                    .NET

  Agent runtime                       .NET and/or Python, depending on
                                      agent and tooling requirements

  Frontend                            Angular

  Relational storage                  PostgreSQL

  Semantic retrieval                  pgvector

  Event processing                    Kafka or a suitable message broker

  Source control                      GitHub API and GitHub Apps

  AI providers                        Provider abstraction for OpenAI,
                                      Anthropic, and other compatible
                                      services

  Observability                       New Relic, Datadog, Splunk, and
                                      Kibana integrations

  Deployment                          Docker; cloud deployment strategy
                                      to be defined
  -----------------------------------------------------------------------

The architecture is intended to evolve as the product is validated.
Technology choices will be driven by reliability, security, operational
complexity, and the needs of the first usable workflow.

------------------------------------------------------------------------

## Security & Human Oversight

Thomas is designed to assist engineering teams, not to become an
unrestricted production operator.

Important safeguards include:

-   **Isolated execution:** code analysis and remediation run in
    controlled workspaces.
-   **Least-privilege access:** integrations receive only the
    permissions needed for their tasks.
-   **Pull requests instead of direct deployment:** proposed changes
    remain reviewable.
-   **Human approval:** merging and production deployment remain under
    the team's control.
-   **Secret protection:** credentials and tokens must be stored
    securely and excluded from prompts, logs, and repository artifacts.
-   **Untrusted input handling:** logs, stack traces, issue
    descriptions, and repository content are treated as untrusted data
    to mitigate prompt-injection risks.
-   **Auditability:** investigations, tool calls, proposed changes, and
    validation results should be traceable.
-   **Configurable policies:** teams can restrict which repositories,
    files, commands, and remediation actions are allowed.

No AI-generated fix should be considered safe merely because it passes a
model's own reasoning. Validation and human review remain essential.

------------------------------------------------------------------------

## Roadmap

The roadmap is organized around delivering one complete workflow before
expanding the platform.

### Phase 1 --- Error to Pull Request

-   [ ] GitHub repository connection.
-   [ ] First observability integration.
-   [ ] Error ingestion and normalization.
-   [ ] Incident investigation workflow.
-   [ ] Repository context retrieval.
-   [ ] AI-generated remediation proposal.
-   [ ] Isolated branch and workspace execution.
-   [ ] Configurable test and validation commands.
-   [ ] Pull request creation with evidence and explanation.

### Phase 2 --- Intelligence & Memory

-   [ ] Structured incident history.
-   [ ] Error signature correlation.
-   [ ] Recurrence detection.
-   [ ] Knowledge base and semantic retrieval.
-   [ ] Resolution outcome tracking.
-   [ ] Evidence-based prioritization.
-   [ ] Confidence and uncertainty reporting.

### Phase 3 --- Multi-Project Platform

-   [ ] Multiple repositories and services.
-   [ ] Additional observability integrations.
-   [ ] Team and code-owner management.
-   [ ] Centralized incident dashboard.
-   [ ] Email and collaboration notifications.
-   [ ] Configurable policies and permissions.
-   [ ] Usage, activity, and effectiveness metrics.

### Phase 4 --- Advanced Agent Workflows

-   [ ] Multi-step investigation plans.
-   [ ] Cross-service incident correlation.
-   [ ] Deployment-aware analysis.
-   [ ] Specialized agents for different investigation tasks.
-   [ ] Evaluation datasets for diagnosis and remediation quality.
-   [ ] Feedback loops based on human review and actual outcomes.

------------------------------------------------------------------------

## Example: An Incident Becomes a Pull Request

Imagine a service begins producing repeated database timeout exceptions
after a deployment.

Thomas would aim to:

1.  Detect the increase in timeout errors.
2.  Group related events and identify the affected service.
3.  Retrieve relevant traces, deployment context, and stack traces.
4.  Inspect the connected repository and relevant code history.
5.  Compare the incident with previous investigations.
6.  Form and explain a root-cause hypothesis.
7.  Prepare a candidate fix in an isolated branch.
8.  Run the configured tests and checks.
9.  Open a pull request with the change, evidence, and validation
    results.
10. Notify the responsible owners and wait for human review.

The pull request should communicate not only **what changed**, but also
**why Thomas believes the change addresses the incident** and **what
remains uncertain**.

------------------------------------------------------------------------

## Project Status

Thomas is currently a product concept and planned engineering project.
The architecture, workflows, and roadmap in this document describe the
intended direction; they should not be interpreted as a list of already
available features.

The initial focus is to validate the complete **error-to-pull-request**
workflow with one source of observability data and one GitHub repository
before expanding integrations and advanced memory capabilities.

------------------------------------------------------------------------

## Contributing

Contribution guidelines, development setup, and project-specific
standards will be added as the implementation takes shape.

Ideas, architecture discussions, and feedback around safe AI-assisted
incident remediation are welcome.

------------------------------------------------------------------------

## License

The project license will be specified in the repository's `LICENSE`
file.

------------------------------------------------------------------------

**Thomas --- Understand the error. Explain the cause. Propose the fix.**

*Built for engineering teams, with AI assistance and human
accountability.*
