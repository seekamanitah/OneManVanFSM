# Full Functionality & UX Simulation Test Report – OneManVanFSM Mobile – 2025-06-18

## Summary
Overall usability score: **6.5/10**. The app has a strong foundation with comprehensive FSM feature coverage, clean card-based UI, and logical navigation. However, multiple functional gaps, timezone inconsistencies, missing feedback loops, and UX friction points across critical user journeys reduce confidence for production readiness. Key themes: (1) static time displays that never update, (2) inconsistent UTC vs local time comparisons throughout, (3) no delete confirmations, (4) several unreachable or dead-end navigation paths, (5) missing keyboard/Enter key support on forms, (6) notification system is synthetic and not persisted, (7) dark mode has no CSS support. Risk level: **Medium-High** — core happy-path flows work, but edge-case and power-user scenarios will surface visible bugs quickly.

---

## Critical Functional Breaks

| # | Severity | Description | Steps to Reproduce | Location | Impact |
|---|----------|-------------|---------------------|----------|--------|
| 1 | **CRITICAL** | Dashboard shift clock display never auto-updates. `FormatElapsed()` computes elapsed time from `DateTime.Now - ClockInTime` but only re-renders on user interaction. A technician clocked in sees a frozen "00:00" or stale time until they tap something. | 1. Clock in on Dashboard. 2. Wait 60 seconds without interacting. 3. Observe the clock display — it stays static. | `Home.razor` line 107, `FormatElapsed()` line 639-644 | **Technicians cannot see real-time shift duration** — core time-tracking feature appears broken to every user. |
| 2 | **CRITICAL** | Job detail page job timer also never auto-updates. Same `FormatJobElapsed()` pattern — computed value is never refreshed with a timer. | 1. Open a job detail. 2. Start job timer. 3. Observe display — frozen at start time. | `JobDetail.razor` line 111 | Per-job time tracking appears non-functional to the field user. |
| 3 | **CRITICAL** | `MobileLayout` shows "All data synced" toast every time ANY page loads — even on first app launch, even before any sync ever occurred. `_showSyncToast = true` and `_lastSyncTime = DateTime.Now` are unconditionally set in `OnInitializedAsync`. | 1. Fresh install → login → navigate to Dashboard. 2. "All data synced" toast appears. 3. No sync has ever occurred. | `MobileLayout.razor` lines 122-123 | **False confidence** — user believes data is current when it may have never synced. Misleading for local-only mode users. |
| 4 | **CRITICAL** | Permission fallback defaults to showing ALL features if role can't be determined: `_canView[f] = true` for every feature. If auth session has a malformed or unrecognized role string, the entire permission system is bypassed. | 1. Create user with custom/misspelled role string. 2. Login → Dashboard → observe all Quick Action buttons visible. | `Home.razor` lines 616-620 | Security: any user with an unrecognized role gets Owner-level feature access on mobile. |
| 5 | **CRITICAL** | Profile page `ClockIn()` has no error handling — catches no exceptions. If `TimeService.ClockInAsync()` throws (e.g., already clocked in, no employee record), the page silently breaks with no feedback. Compare to `Home.razor` which properly catches `InvalidOperationException`. | 1. Go to Profile. 2. Try clocking in when already clocked in or with no employee link. 3. Clock button does nothing. No error shown. | `Profile.razor` lines 262-266 | User is stuck on profile page unable to clock in/out with no explanation. |

---

## High Priority Issues

| # | Severity | Description | Steps to Reproduce | Location | Impact |
|---|----------|-------------|---------------------|----------|--------|
| 6 | **HIGH** | No form submission on Enter key for Login page. Pressing Enter after typing password does nothing — only clicking the "Sign In" button works. | 1. Go to login page. 2. Type username and password. 3. Press Enter key. 4. Nothing happens. | `Login.razor` — form is `<div>` elements, not `<EditForm>` or `<form>` with `@onsubmit`. | Every user attempts Enter to login. Failed expectation = poor first impression. |
| 7 | **HIGH** | Delete operations have NO confirmation dialog: Notes delete, Documents delete, Expense delete all fire immediately on tap. One misclick permanently destroys data. | 1. Notes page → tap trash icon on any note → note instantly deleted. Same for Documents, Expenses. | `Notes.razor` line 112, `Documents.razor` line 104-106, `Expenses.razor` line 141 | **Data loss** — single misclick on a small mobile touch target deletes records with no undo. |
| 8 | **HIGH** | Notifications page constructs synthetic notifications from dashboard data — they are not persisted. Every page load regenerates them. "Mark All Read" only works for current session — on refresh, everything resets to unread. | 1. Open Notifications → "Mark All Read". 2. Navigate away. 3. Return to Notifications → all items are unread again. | `Notifications.razor` lines 131-289 | Notification state is meaningless. Users cannot trust "read" status. |
| 9 | **HIGH** | Mixed UTC/Local time comparisons throughout the app. `RelativeDateLabel` in Jobs uses `DateTime.UtcNow.Date` to compare against `ScheduledDate` (which may be local). `Notifications.razor` date grouping compares against `DateTime.UtcNow.Date`. `Inventory.razor` expiry check uses `DateTime.UtcNow`. `AssetDetail.razor` warranty calculations all use `DateTime.UtcNow`. But `MobileCalendar.razor` correctly uses `DateTime.Now`. This inconsistency means: (a) "Today" labels may be wrong near midnight in non-UTC timezones, (b) Items may appear overdue when they're not, or vice versa. | 1. Set device to UTC-8 timezone. 2. At 5 PM local (1 AM next day UTC), check Jobs list. 3. Today's jobs may show as "Yesterday" due to UtcNow comparison. | `Jobs.razor` lines 377, 390; `Notifications.razor` lines 54, 58, 147; `Inventory.razor` line 74; `AssetDetail.razor` lines 67, 68, 77, 90-91, 102-103, 114-115 | Dates and "Today"/"Overdue" labels will be wrong for ~33% of the daily cycle for users not in UTC. |
| 10 | **HIGH** | Notification badge dot in CSS (`.notification-badge::after`) shows a red dot permanently via CSS pseudo-element — it always appears regardless of whether `_notifCount` is 0. The conditional count span (line 22-25 in MobileLayout) correctly shows/hides, but the CSS pseudo-element red dot is always visible. | 1. Login with user that has 0 overdue jobs, 0 pending notes, 0 low stock, 0 expiring agreements. 2. Observe bell icon — still has a red dot. | `app.css` lines 66-76, `MobileLayout.razor` line 20-25 | Users always think they have notifications. "Crying wolf" effect kills trust in the badge. |
| 11 | **HIGH** | "Maintenance Due" and "Warranty Alert" cards on the Dashboard have no `@onclick` handler — they are dead cards. Every other alert card navigates to its respective page. | 1. Dashboard → observe "PM Due" or "Warranty" alert card. 2. Tap it. 3. Nothing happens. | `Home.razor` lines 214-219 (Maintenance), 220-225 (Warranty) | Dead interaction pattern. User sees cursor:pointer but gets no result. Broken consistency with neighboring cards. |
| 12 | **HIGH** | Dark mode setting has no CSS implementation. `Settings.razor` allows toggling Theme to "Dark", and `MobileLayout` sets `data-theme="dark"` on the document, but `app.css` has zero `[data-theme="dark"]` selectors or dark mode color variables. | 1. Settings → Theme → Dark. 2. Observe: nothing changes. App stays light. | `app.css` (entire file), `Settings.razor` line 33 | Feature is advertised but does nothing. User switches to dark and thinks the app is broken. |
| 13 | **HIGH** | `user-scalable=no` in `index.html` viewport meta tag prevents pinch-to-zoom. This is an accessibility violation (WCAG 1.4.4) — users with low vision cannot zoom to read small text (and the app has many text elements at 0.6-0.7rem). | Review `index.html` line 5: `user-scalable=no` | `wwwroot/index.html` line 5 | Accessibility barrier. Also frustrating for any user trying to read tiny stat labels. |

---

## Medium UX Frictions

| # | Description | Location | Impact |
|---|-------------|----------|--------|
| 14 | Jobs search fires `LoadJobs()` on every keystroke via `@bind:event="oninput"` with `@bind:after="LoadJobs"` — no debounce. Each character triggers a full database query. Search page correctly debounces (300ms timer), but Jobs page does not. | `Jobs.razor` lines 27-28 | Laggy typing experience; excessive DB calls especially for slow local SQLite queries. |
| 15 | Documents and Products search also fires on every keystroke with `@bind:after="LoadDocuments"` / `@bind:after="LoadProducts"` — same no-debounce issue. | `Documents.razor` lines 25-26, `Products.razor` lines 88-89 | Same performance concern as #14. |
| 16 | Notes search binds with `@bind:event="oninput"` but filtering is client-side (`FilteredNotes` property). However, the UI does not call `StateHasChanged()` or trigger re-render on category change — `SetCategory` changes `_categoryFilter` but doesn't call `LoadNotes()`. The property recomputes on next render, but there's no explicit trigger. | `Notes.razor` lines 223-226 | Category filter chip selection may appear unresponsive — user taps a chip but list doesn't visually update until next interaction. |
| 17 | Login page logs the username in plain text to diagnostics panel: `$"Login attempt: user='{_username.Trim()}'"`. If the diagnostics panel is visible (and it persists in memory), another person looking at the screen can see the username. | `Login.razor` line 221 | Minor PII exposure — security concern for shared devices. |
| 18 | Setup (first-time password change) page has no "show password" toggle. User must blindly type the current password, new password, AND confirm password — three password fields with no visibility option. | `Setup.razor` lines 38-53 | High frustration for first-time users, especially on mobile keyboards where typos are common. |
| 19 | Reports page "Back to Profile" button always navigates to `/profile` — not browser back. If the user arrived from a Quick Actions link on Dashboard, pressing Back takes them to Profile (unexpected), not back to Dashboard. | `Reports.razor` line 16 | Confusing navigation — "Back" doesn't go where user came from. |
| 20 | Settings page "Back to Profile" has same issue — hardcoded to `/profile`. | `Settings.razor` line 20 | Same as #19. |
| 21 | Profile page "Back" button navigates to `/` (dashboard). If user came from the bottom nav "Me" tab, pressing back skips the natural back-button expectation. Not broken, but creates navigation inconsistency. | `Profile.razor` line 274 | Minor cognitive dissonance. |
| 22 | Bottom nav has 6 items but Calendar is not easily reachable — it's between Jobs and Invoices but has no dedicated prominent placement. The quick action grid on Dashboard also doesn't include a Calendar shortcut in the first row. | `MobileLayout.razor` lines 42-67 | Calendar discoverability issue for novice users. |
| 23 | Estimate detail view: "Approve & Create Job" button text suggests one action but performs two (approve estimate + create job). No confirmation of what will happen. For elevated roles, this silently creates a job and changes estimate status. | `Estimates.razor` lines 193-197 | Accidental job creation from estimate approval — irreversible combined action. |
| 24 | `FocusOnNavigate` in `Routes.razor` targets `h1` selector, but no page in the app uses `<h1>` tags. Pages use `<h4>`, `<h5>`, `<h6>` or no heading elements. Focus never moves to the page content on navigation. | `Routes.razor` line 6 | Accessibility: screen reader users don't get focus moved to main content after navigation. |
| 25 | `mobile-card:active { transform: scale(0.98) }` applies to ALL cards, including those that aren't clickable (e.g., static info cards, stat cards without onclick). This creates a "clickable" visual affordance on non-interactive elements. | `app.css` lines 186-189 | Confusing — user thinks card is interactive but it does nothing. |
| 26 | Priority dot CSS class uses `.priority-standard` but the `Priority` enum values used in code are `Normal`, not `Standard`. The dot for Normal priority jobs renders with no color (no matching CSS). | `app.css` line 258: `.priority-standard`, `Jobs.razor` line 57: `priority-@job.Priority.ToString().ToLower()` which generates `priority-normal` | Normal-priority jobs have invisible priority dots. |
| 27 | Search page debounce timer is never disposed. The `_debounceTimer` is a `System.Timers.Timer` created on each keystroke but the page doesn't implement `IDisposable` to clean it up. | `Search.razor` lines 159, 185-201 | Timer leak — each search keystroke allocates a new timer. Accumulated timers may fire after page disposal. |
| 28 | Recent searches are stored with `|` separator but no escaping. If a user searches for a query containing `|`, the storage/parsing will corrupt all saved searches. | `Search.razor` lines 177, 182 | Edge case data corruption for search history. |
| 29 | Settings page notification toggles (`NotificationsEnabled`, `JobAlerts`, etc.) bind with `@bind` but have no save trigger — there's no "Save Settings" button visible. Theme has `@bind:after="OnThemeChanged"` but notification settings appear to silently auto-save (or not save at all if the service doesn't persist on property change). | `Settings.razor` lines 49, 59, 70, 81 | User toggles settings but unclear if they're saved. No confirmation feedback. |
| 30 | Expense create/edit form: Amount and Tax fields are separate, but Total is not shown during editing. User must mentally compute Amount + Tax. The detail view shows the split, but the form doesn't show a live computed total. | `Expenses.razor` lines 177-184 | Cognitive load during expense entry — "How much will this total?" |
| 31 | SyncLogs page shows error logs only — no success logs, no sync history. If a user syncs successfully, the page shows "No sync errors recorded" which doesn't confirm sync happened. | `SyncLogs.razor` lines 35-39 | Users can't verify sync history — only see errors. No positive confirmation. |

---

## Low / Polish Items

| # | Description | Location |
|---|-------------|----------|
| 32 | Several pages have `cursor:pointer` set via inline styles on non-interactive elements or elements that only sometimes have an `@onclick`. E.g., Maintenance Due and Warranty alert cards have `cursor:pointer` but no click handler. | `Home.razor` lines 215, 222 |
| 33 | `ScheduledTime` displays use `@"hh\:mm"` TimeSpan format which shows hours with leading zero but no AM/PM — e.g., "02:30" could be 2:30 AM or PM. This format is ambiguous for users. | `Home.razor` line 193, `Jobs.razor` line 80 |
| 34 | Notification badge count formula (`overdue + pending notes + low stock + expiring agreements`) loads the entire dashboard just for a badge count. On every `OnInitializedAsync` of the layout. | `MobileLayout.razor` lines 146-158 |
| 35 | `Companies.razor` detail view checks `Id > 0` instead of `Id.HasValue`. If `Id` is nullable int (which it should be from route parameter), `Id > 0` would throw on null. Inconsistent with other pages like `ServiceAgreements.razor` which uses `Id.HasValue`. | `Companies.razor` line 6 |
| 36 | Product "Quick Add" form allows negative cost and price values (`<input type="number" step="0.01">`). No `min="0"` constraint. Same for inventory edit form. | `Products.razor` lines 29, 35; `Inventory.razor` lines 164, 170, 177 |
| 37 | Login page diagnostic log shows "App registered as: REMOTE" or "LOCAL" based on `_isRemoteMode`, but this state is computed before connection test — may be stale. | `Login.razor` line 154 |
| 38 | `Setup.razor` password minimum is 6 characters — displayed as placeholder "Min. 6 characters" but weak for production. No complexity requirements shown. | `Setup.razor` line 47, 92 |
| 39 | Calendar week view LINQ query `.Where(e => e.StartDateTime.Date == day.Date)` runs inside a for-loop of 7 days, repeatedly filtering the same list without pre-grouping. Minor inefficiency. | `MobileCalendar.razor` line 122 |
| 40 | Bottom nav label font size is 0.58rem (about 9.3px) — below WCAG minimum of 12px for readable text. `stat-label` is 0.62rem. Many other elements dip below 10px. | `app.css` line 130, 233 |
| 41 | `goBack()` JS function checks `window.history.length > 1` but this is unreliable — `history.length` includes forward-navigated entries and doesn't accurately reflect whether back-navigation is available. | `wwwroot/js/helpers.js` lines 6-8, 11-17 |
| 42 | Invoice item picker (added in previous session) creates a bottom-sheet overlay but has no swipe-to-dismiss or backdrop-click-to-close behavior. Only the × button closes it. | `Invoices.razor` (item picker overlay) |
| 43 | No pull-to-refresh gesture support on any page. Users must find and tap a small refresh icon button. | All pages |
| 44 | Profile page shows "Technician" as role suffix for all roles: "Owner Technician", "Admin Technician", "Manager Technician". | `Profile.razor` line 29 |

---

## User Journey Pain Points

### 1. First-Time User Onboarding (Novice)
**Flow:** Install → Open → Login → Setup (password change) → Dashboard  
**Pain:** Login form doesn't respond to Enter key (must tap button). Setup page has 3 blind password fields with no show/hide toggle. After changing password, user lands on Dashboard that shows "All data synced" (false — nothing has synced). Dashboard shows 0/0/0 stats with no explanation of what to do next. No onboarding tour, tooltips, or "Get Started" guidance. The "Load Demo Data" option is buried in Settings, which is buried behind Profile → Settings. **A novice has no idea how to populate the app with data.**

### 2. Technician Daily Workflow (Core User)
**Flow:** Open app → Clock In → Check jobs → Navigate to first job → Start job timer → Complete work → Clock Out  
**Pain:** Shift clock display never updates — user doesn't know if clock-in worked. Must navigate to Profile or refresh Dashboard to see current time. Job timer on the detail page also never updates. Changing job status from Dashboard (En Route → On Site → Complete) reloads the entire dashboard each time, causing scroll position loss. If user was scrolled to their 3rd job, they're snapped back to top after each status change.

### 3. Expense Logging (Field Tech)
**Flow:** Dashboard → Expenses (via Quick Actions) → Log Expense → Save  
**Pain:** Must fill amount and tax separately with no running total visible. Category dropdown has 10 options but no search. No receipt photo attachment capability (Documents page exists separately but isn't linked from expense creation). After saving, user is returned to list but can't easily link the expense to a specific job inline — must edit after creation.

### 4. Notification Check (Impatient User)
**Flow:** Tap bell icon → Notifications page → Scan for urgent items  
**Pain:** Page takes time to load because it calls Dashboard service, then Inventory service, then Estimates service, then Agreements service — all sequentially. Mark All Read works but resets on next visit. Red notification dot persists in header CSS even with 0 notifications. User can never clear the badge. Tapping a notification navigates to the relevant page but there's no way to get back to the notification list (no "back to notifications" — relies on browser back which may not work due to WebView history issues).

### 5. Offline/Remote Sync (Power User)
**Flow:** Configure remote server → Sync → Verify data  
**Pain:** Test Connection button on login doesn't validate URL format — user can enter garbage and hit Test. Sync Now on Dashboard gives result message but auto-clears after 4 seconds — if user blinks, they miss it. Sync Logs page only shows errors, not successes. No way to see what was synced (entities, counts, timestamps per entity type). Offline queue in Settings shows pending items but truncates to 5 with "N more" — no way to see all pending items.

### 6. Deleting Records (Edge-Case / Frustrated User)
**Flow:** Any page with delete capability → Tap delete → Record immediately gone  
**Pain:** No confirmation on any delete action (Notes, Documents, Expenses). One accidental tap deletes a customer note or job document permanently. No undo. No "Archive" alternative. This is especially dangerous on mobile where fat-finger taps on small icons (32px min-height buttons next to edit buttons) are common. A frustrated user rapidly tapping could delete the wrong item.

### 7. Searching Across the App (Power User)
**Flow:** Header search icon → Search page → Type query → Scan results  
**Pain:** Search is global but categories are limited to "Jobs, Customers, Sites, Assets, Notes" — doesn't include Invoices, Estimates, Products, Inventory, Expenses, Documents, or Agreements. Power users looking for an invoice number via search will get zero results even if the invoice exists.

### 8. Dark Mode Usage (Accessibility-Needs User)
**Flow:** Profile → Settings → Theme → Dark → Use app  
**Pain:** Toggle works (setting is saved, attribute is set on HTML), but absolutely nothing changes visually. No dark mode CSS exists. User with light sensitivity or night-use preference gets no benefit. They may think the app is broken.

---

No code was suggested, written or fixed. Only problems observed during simulated real usage were reported.
