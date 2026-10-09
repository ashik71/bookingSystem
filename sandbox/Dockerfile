# SlotBook sandbox image: .NET 10 SDK + Node + git + gh + Claude Code.
#
# Two users, on purpose (see sandbox/README.md):
#   root  — runs entrypoint.sh: clones, fetches context, pushes, posts comments.
#           Holds the GitHub token. Runs with almost every capability dropped.
#   agent — runs Claude Code. Holds only the Claude token; never sees GH_TOKEN.
#
# Build: sandbox/run-job.sh builds it automatically when these files change.

ARG DOTNET_SDK_IMAGE=mcr.microsoft.com/dotnet/sdk:10.0
ARG NODE_IMAGE=node:24-bookworm-slim

FROM ${NODE_IMAGE} AS node

FROM ${DOTNET_SDK_IMAGE}

ARG CLAUDE_CODE_VERSION=2.1.295

# Node: copy the official build rather than piping an installer into a shell.
COPY --from=node /usr/local/bin/node /usr/local/bin/node
COPY --from=node /usr/local/lib/node_modules /usr/local/lib/node_modules
RUN ln -s ../lib/node_modules/npm/bin/npm-cli.js /usr/local/bin/npm \
 && ln -s ../lib/node_modules/npm/bin/npx-cli.js /usr/local/bin/npx \
 && ln -s ../lib/node_modules/corepack/dist/corepack.js /usr/local/bin/corepack \
 && node --version && npm --version

# gh from GitHub's signed apt repo; jq and ripgrep for scripts and the agent.
RUN apt-get update \
 && apt-get install -y --no-install-recommends ca-certificates curl gnupg jq ripgrep git \
 && install -d -m 0755 /etc/apt/keyrings \
 && curl -fsSL https://cli.github.com/packages/githubcli-archive-keyring.gpg \
      -o /etc/apt/keyrings/githubcli-archive-keyring.gpg \
 && chmod a+r /etc/apt/keyrings/githubcli-archive-keyring.gpg \
 && echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/githubcli-archive-keyring.gpg] https://cli.github.com/packages stable main" \
      > /etc/apt/sources.list.d/github-cli.list \
 && apt-get update \
 && apt-get install -y --no-install-recommends gh \
 && rm -rf /var/lib/apt/lists/*

# The agent user. Cache dirs are pre-created so named volumes mounted there
# inherit agent ownership on first use.
RUN useradd --create-home --shell /bin/bash --uid 1001 agent \
 && install -d -o agent -g agent /work/agent /work/out \
      /home/agent/.nuget /home/agent/.nuget/packages /home/agent/.npm \
 && install -d -m 0755 /work/context

USER agent
ENV PATH=/home/agent/.local/bin:$PATH \
    DISABLE_AUTOUPDATER=1 \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1
RUN curl -fsSL https://claude.ai/install.sh | bash -s ${CLAUDE_CODE_VERSION} \
 && claude --version

USER root
COPY entrypoint.sh /usr/local/bin/slotbook-job
RUN chmod 0755 /usr/local/bin/slotbook-job

WORKDIR /work
ENTRYPOINT ["/usr/local/bin/slotbook-job"]
