# Changelog

## Version 1.2.1 - 2026-02-15

### Added
- Cursor-Following Glow — White spotlight follows mouse (top-left + bottom-right)
- Custom Scrollbar — Teal-themed thin scrollbar across the app

### Improved
- How It Works Flow — Sequential 5s animation: INPUT fills → passes to PROCESS → OUTPUT
- Log Details Modal — Full-screen view with consistent level-tag colors (INFO, ERROR, WARN, DEBUG)
- Get Started Navigation — Clicking Get Started or selecting a file now redirects to Logs page

### Fixed
- Log Details Level Tags — Modal now displays colored badges matching the main table

## Version 1.2.0 - 2026-01-21

### Added
- Auto-Update System - Automatic background updates with progress bar
- Smart Download - Fallback to direct download if auto-update fails
- Update Notifications - In-app alerts for new versions
- Version Display - Current version shown in Settings

### Improved
- Update Reliability - Enhanced error handling for network issues
- User Experience - Non-blocking update checks
- Security - Verified update sources

### Fixed
- Update Errors - Resolved issues with update checks
- UI Glitches - Fixed update status display

## Version 1.1.9 - 2026-01-21

### Added
- New Premium UI Design with glassmorphism and neon accents
- Advanced Analytics Guard - prevents empty charts with a smart modal
- RabbitMQ & IIS Log Type Support (Coming Soon placeholders)
- Automatic Navigation - app now switches to Logs view after file selection

### Improved
- UI Performance & Interaction - smoother transitions between views
- Refactored Sidebar Navigation - robust event delegation for zero-lag switching
- Responsive Glassmorphism - improved blur effects and border consistency
- Persistent Parser Type - selected type is now always visible in the sidebar

### Fixed
- Sidebar "Ghost" States - fixed bug where multiple items appeared active
-  Broken Assets - restored missing CSS and script links in build process
-  UI Glitches - resolved flickering during rapid tab switching

## Version 1.1.8 - 2026-01-20

### Added
- Installation Log Parser - Full support for installation logs with timestamp extraction
- Tabbed Modal View - Message and Stack Trace tabs for detailed log inspection
- Clickable Stat Cards - Quick filtering by clicking on Total/Errors/Warnings/Info
- Smart Date Detection - Automatically extracts date from filename or uses file modification date

### Improved
- Display up to 10,000 log entries (previously 500)
- Better timestamp formatting
- Fixed filter dropdown visibility
- Enhanced modal UI with smooth animations

### Fixed
- Embedded mode no longer prompts for user input
- Proper statistics calculation by log level
- Correct date parsing from log files

---

## Version 1.0.0 - 2026-01-05

### Added
- First public release
- Basic log parsing functionality
- Modern UI with dark theme
