_Changes for v 1.19_:
- Android: added refresh button that allows user o reset notifications state and make them re-request their texts again (as so as at the start of the app);
- Android: updated buttons names on the master notification;
- Some code improvements applied;
- Android: added message offset for sharing function; it allows to share up to 5 first notifications from the main log;
- Android: app will not start the background service on share button pressing (in previous version this led to unpredictable result of calling of last notification's text);
- Android: app will stop the background service if it's already started, but corresponding switch is off;
- Android: log field is now customizable: read mode and font size now can be specified by user
