_Changes for v 2.2.3_:
- Notifications processing fixup (eliminates repeating messages with the same texts);
- Third try to fix app failure on long list of unchanged notifications:
    - only 5 unchanged notifications will be processed in a row;
    - single entrance protection moved to GetHTML method;
    - GetHTML method now releases resources properly;
- Android: background service will now be properly terminated on app resume (when it haven't been closed);
- Android: some interface fixes applied;
- Added ability to share notification settings as parameters strings; this string may be received by used (through any possible channel), copied to clipboard and loaded by UniNotifier
