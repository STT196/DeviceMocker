# DeviceMocker Marketplace Draft

This file is the marketplace-facing product draft for DeviceMocker.

For the full current release copy, see:

- [docs/MARKETPLACE_DRAFT_v1.1.0.md](docs/MARKETPLACE_DRAFT_v1.1.0.md)

## Product Name

DeviceMocker

## Tagline

Windows toolkit for hardware input simulation and POS printer/cash drawer development workflows.

## Short Description

DeviceMocker helps developers test hardware-dependent software without requiring physical scanners, serial devices, receipt printers, or cash drawers during early development and integration.

## Summary

DeviceMocker is a Windows desktop toolkit built for teams developing POS systems and other hardware-dependent business software. It supports realistic device simulation for barcode scanners, keyboards, serial workflows, RFID, magstripe, scales, and more, while also providing a focused ESC/POS emulator host for receipt-printer and printer-driven cash drawer development.

This release also includes a companion POS Hardware Test App for sending ESC/POS test traffic into the emulator host during local development.

## Best Fit

- POS software teams
- retail system integrators
- internal business systems with hardware dependencies
- developers who need repeatable local test workflows without full device labs

## Included Applications

### DeviceMocker

The main desktop application for:

- hardware input simulation
- profile-based testing
- ESC/POS emulator hosting
- printer and drawer workflow development

### POS Hardware Test App

The companion desktop application for:

- sending ESC/POS receipt text
- sending drawer-kick commands
- testing cut commands
- sending raw hex payloads
- validating host behavior during development
