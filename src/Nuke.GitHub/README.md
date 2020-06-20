# Release

## Nuke
- get the MD for the latest release
- create a pull request with that document added to another repository
    1. First Option
        - clone target website repository
        - download
    2. repository dispatch events
        - dispatch an event to another repository
        - receive the event
        - do work


## Repository Dispatch Event on Release

1. Create the Repository Dispatch Event on Release
    - https://help.github.com/en/actions/reference/events-that-trigger-workflows#external-events-repository_dispatch
    - https://developer.github.com/v3/repos/#create-a-repository-dispatch-event
    - https://help.github.com/en/actions/reference/events-that-trigger-workflows#release-event-release
    - https://developer.github.com/v3/repos/releases/#get-a-release
    
2. Receive an Event
    - https://help.github.com/en/actions/reference/events-that-trigger-workflows#external-events-repository_dispatch
    - https://help.github.com/en/actions/reference/events-that-trigger-workflows#release-event-release
    - https://developer.github.com/webhooks/event-payloads/#release
3. Create Document
4. Create Pull Request
5. Merge