# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

<!---
To easily get a list of committed changes between current master and the previous release use:
git log --oneline --no-decorate --topo-order ^<previousRelease> master
where <previousRelease> is the release name e.g 5.1.0
-->

## [5.4.4] - 2021-08-31
### Add
 - React version upgrade changes (#334)
 - Display Device Twin data in tables using new Column Mapping and Column Options
 - Create Device Groups by DeviceId or DeviceName
 - Expired sessions no longer require loging off
 - Added audit data under User Management

### Fix
 - Non-compliant resources based on security recommendations
 - Fixed Getlink issue on Device Explorer when using "Load more"
 - Added changes for refreshing cache


## [5.4.3] - 2020-02-02
### Add
 - Download data in Device Explorer and Device Search
 - Show Previous Firmware Version in Deployment History
 - Links between Device History (or firmware updates) and Deployments
 - New Firmware Template Variables for use in Packages

### Fix
 - Resolved issues with Device History data
 - Renamed 3rd column in Device History to Date instead of Last Update Date
 - Ensurced ssh-keygen to generate keys in PEM format in CI/CD

## [5.4.2] - 2020-01-18
### Add
- UI now supports unlimited devices
- Start/stop toogle for loading additional devices with stats in toolbar
- Device Search page to manage devices independent of Device Group
- Highlights current tenant of settings flyout
- Displays IoT Hub name on settings flyout
- Updated documentation links
- TenantId added to GetLink URL

### Fix
- Deployment now updates sys admins
- Adds System Admins on new tenant creation
- Improvements to cToken changes
- Fixed add rule issue in WebUI
- Readers can now change Device Group
- JSON web keys now use=sig

## [5.4.1] - 2020-10-14
### Fix
- Fixed performance issues reading "deleted" rules
- Fixed rule deletion to delete rules
- Upgraded versions of several dlls harden security 
- Fixed rule and alarms 502 issue
- Enabled grid cell text highlight
- Addressed auto refresh on settings flyout
- Force support for HTTPS in template

## [5.4.0] - 2020-10-05
### Add
- Deployment history by Device
- New device telemetry comparison tool
- Direct links to flyout panels
- Migrated Azure Functions to Open Source Repo
- Made flyouts expandable
- Environment specific blue/green configuration and deployment

### Fixed
- Made toolbar buttons responsive
- Added expand columns to grid
- Integrated whiteSource bolt with test pipeline for enhanced security checks
- Update last refresh component to show only refresh icon
- Added tooltips to grid values on hover
- Added test accounts to deployment process for automated tests
- Shifted searchbox location
- Tooltip on hover over device group name

## [5.3.0] - 2020-09-10
### Add
- UI Enhancements for enabling / disabling active deployments, those impacting deployment quotas
- Downloadable deployment reports
- Nested Device Properties Editable in UI (up to 6 levels)
- Get URLs that include DeviceGroup filtering
- Auto-refresh UI cache after deployment
- Improvements to tenant lifecycle

### Fixed
- Reported Properties Changes in Deployment Details
- Fixed UI continuous refresh issues when alerting is off
- Added changes to handle UNIX timestamp
- Sys-admins can access to all tenants
- Prevent users from filtering dashboard duration that does not have telemetry data

## [5.2.3] - 2020-08-18
### Fixed
- Asa-manager twin update job properly triggers device group conversion
- Updated methods to throw expectation when there is no collection
- Prevent Azure Function calls when alerting is disabled
- Updated ResourceNotFoundException in Rules Methods
- Removed telemetry entries in health probes to reduce logging costs

### Added
- Improved control over tracking and managing the deployments imposed by IoT Hub


## [5.2.2] - 2020-07-26
### Fixed
- Added and updated translations for phrases for German, English, French, Spanish, Hindi, Tamil, and Vietnamese
- Pinned device groups are now properly saved
- Active device group now switches on new session
- Fixed issues with alerts being disabled for rules affecting device groups with numeric conditions
- Re-enable cross-partition queries for alarms
- Location for DPS now uses configuration value instead of hard-coded "eastus"

## [5.2.1] - 2020-07-15
### Fixed
- Corrected the application version number

## [5.2.0] - 2020-07-15
### Added
- Show application version number in settings flyout with link to changelog for release notes
- Updated the display of device names in telemetry chart
- Device group sorting
- Package firmware JSON template is now fully customizable with a configurable default
- Access device file uploads in device details flyout
- Allow the creation of supported methods per device group
- Timeframe for telemetry chart in device details flyout is now configurable
- Telemetry chart displays explanation when incomplete dataset is shown due to message count limits
- Enable configuration of device telemetry message retrieval count limit

### Fixed
- Re-enable cross-partition queries for device telemetry messages
- Clarified language in system settings privacy notice
- Telemetry chart attributes no longer show unnecessary left-right scroll buttons
- Enabling advanced alerting in the settings panel no longer shows as failed
- Default logo now appears with correct size
- Prevent stack trace from appearing in error message when API returns HTTP 500
- Now default to read batched data timestamp in seconds in ASA
- Sign-in to Outlook is no longer required for enabling emails on rules
- Prevent occasional blank screen in Edge browser
- Custom role names now appear correctly in User Profile flyout and elsewhere

## [5.1.0] - 2020-06-12
### Added
- Cache IoT Hub device twin query results to greatly reduce throttling and latency
- Clicking a package name in the Packages page now displays the package JSON data
- Improved logo formatting and styles

### Fixed
- Custom fields added to package JSON are now properly persisted and deployed
- Alerting infrastructure now properly configured to send emails when alerts trigger

## [5.0.1] - 2020-06-09
### Fixed
- Use local timezone for time display in telemetry chart
- Greatly reduce frequency of UI rendering errors resulting from IoT Hub query throttling
- New deployments now have package name and version
- Add missing files related to package management that were lost when repository was transplanted

## [5.0.0] - 2020-05-22
### Added
- Multi-tenancy: sandboxed IoT environments within a single deployment infrastructure but with separate data storage and users per tenant
- Identity Gateway microservice for chaining authentication flow to another OAuth provider
- Azure Pipelines YAML for deploying infrastructure and code

### Changed
- Streaming is now done serverless using Azure Functions
- Application configuration uses Azure App Configuration service in addition to Azure Key Vault
- Code base rearchitected to use common library and reduce duplication

[5.4.4]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.4.4
[5.4.3]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.4.3
[5.4.2]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.4.2
[5.4.1]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.4.1
[5.4.0]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.4.0
[5.3.0]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.3.0
[5.2.3]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.2.3
[5.2.2]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.2.2
[5.2.1]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.2.1
[5.2.0]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.2.0
[5.1.0]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.1.0
[5.0.1]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.0.1
[5.0.0]: https://github.com/3mcloud/azure-iot-platform-dotnet/releases/tag/5.0.0
