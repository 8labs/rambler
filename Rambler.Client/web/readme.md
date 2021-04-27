# Get up and running

Install the dependencies, then run from NPM.  (Running this way doesn't require a global gulp install.  It will use the one in the package.)

    npm install
    npm run watch

Use your http server of choice to serve the 'build' folder.  I use http-server.  

    npm install -g http-server
    http-server ./build -a 0.0.0.0

The default dev config (config/env.ts) is setup to use dev.everywherechat.com as the host address for the server.  Set this up to point to whichever server you want, but browsers can get cranky about the domain names matching.

You can also use the --type=[env_name] switch to use different configs.

