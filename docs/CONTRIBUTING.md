We'll be glad to accept patches and contributions to the project. There are
just few guidelines we ask to follow.

Contribution License Agreement
==============================

If you want/plan to contribute, we ask you to sign a
[CLA](https://cla.microsoft.com/) (Contribution License Agreement).
A friendly bot will remind you about it when you submit a pull-request.

Submitting a contribution
=========================

It's generally best to start by
[opening a new issue](https://help.github.com/articles/creating-an-issue)
describing the work you intend to submit. Even for minor tasks, it's helpful
to know what contributors are working on. Please mention in the initial issue
that you are planning to work on it, so that it can be assigned to you.

Follow the usual GitHub flow process of
[forking the project](https://help.github.com/articles/fork-a-repo),
and setup a new branch to work in. Each group of changes should be done in
separate branches, in order to ensure that a pull request only
includes the changes related to one issue.

Any significant change should almost always be accompanied by tests. Look at
the existing tests to see the testing approach and style used.

## Code style

If you use ReSharper or Rider, you can load the code style settings from
the repository, stored in
[device-simulation.sln.DotSettings](device-simulation.sln.DotSettings)

Some quick notes about the project code style:

1. Where reasonable, lines length is limited to 80 chars max, to help code
   reviews and command line editors.
2. Code blocks indentation with 4 spaces. The tab char should be avoided.
3. Text files use Unix end of line format (LF).
4. Dependency Injection is managed with [Autofac](https://autofac.org).
5. Web service APIs fields are CamelCased (except for metadata).

## Git setup for contributing to the solution

### Commits

Do your best to have clear commit messages for each change, in order to keep
consistency throughout the project. Reference the issue number (#num). A good
commit message serves at least these purposes:
* Speed up the pull request review process
* Help future developers to understand the purpose of your code
* Help the maintainer write release notes

One-line messages are fine for small changes, but bigger changes should look
like this:
```
$ git commit -m "A brief summary of the commit
>
> A paragraph describing what changed and its impact. Lorem ipsum dolor sit
> amet consectetur adipiscing elit ligula, blandit diam cursus vitae potenti
> egestas viverra volutpat sodales, etiam non pharetra hac sociosqu aenean
> primis. Sodales fermentum cras scelerisque interdum cubilia molestie
> convallis curabitur, augue habitasse per felis vitae parturient etiam nulla,
> facilisi vehicula diam eleifend lacus natoque venenatis."
```

Finally, push the commits to your fork, submit a pull request, wait for the
automated feedback from Travis CI, and follow the code review progress. The
team might ask for some
[changes](https://help.github.com/articles/committing-changes-to-a-pull-request-branch-created-from-a-fork)
before merging the pull request.

### Git Hooks

The project includes a pre-commit
[git hook](https://git-scm.com/docs/githooks),
to automate some checks before accepting a code change. You can run the tests
manually, or let the CI platform to run the tests. We use the following git
hook to automatically run all the tests before sending code changes to GitHub
and speed up the development workflow.

If at any point you want to remove the hook, simply delete the `pre-commit`
file installed under `.git/hooks`. You can also bypass the pre-commit hook
using the `--no-verify` option.

To setup the included git hook, open a Windows/Linux/MacOS console and execute:

```
cd PROJECT-FOLDER
cd scripts/git
setup --no-sandbox
```

With this configuration, when checking in files, git will verify that the
application passes all the tests, running the build and the tests in your
workstation, using the tools installed in your OS.

Note: you will need [.NET Core 2](https://dotnet.github.io) installed and
in the system PATH. If you don't want to install .NET Core, you can run
`setup --with-sandbox` instead, so build and tests will run inside
a pre-configured Docker container.
