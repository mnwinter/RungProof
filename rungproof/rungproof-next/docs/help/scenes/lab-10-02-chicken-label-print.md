# Lab 10.2 - Chicken Label Print

Scene ID: `lab-10-02-chicken-label-print`

This offline plant measures a loaded product and executes separately commanded printing and label application. Load & weigh places the default 1.237 kg tray on the weigh deck over 0.3 seconds; measurement becomes stable after 0.5 seconds of running plant time. Reset restores an absent product at off-platform home.

PC feedback: `product_present`, `product_weighed`, `weight_kg` (REAL), `print_complete`, `application_complete`. Manual permissive fixtures: `printer_ready`, `label_data_valid` (initially false).

PLC commands: `print_request`, `apply_request`, `label_text` (STRING, maximum 255 characters). Legacy `label_applied` remains a controller-owned indication, not actual application feedback.

Author or load a controller. It must format the measured weight into label_text, require stable weight and valid printer/data, and raise print_request. The plant captures exact nonblank text on the rising request and prints for 0.6 seconds. Later text changes do not modify that job. Printer/data loss pauses printing. Held print does not duplicate jobs.

After print_complete, raise apply_request separately. Paper travels to the tray over 0.5 seconds and application_complete becomes true. No automatic application or hidden controller is supplied. A rejected edge is not queued; clear and reassert the command after correcting its permissive. Load is blocked during printing/application.

Stop freezes timing and poses. Reset clears feedback, paper and captured job text. These illustrative animations do not certify printer mechanics, adhesive contact, weighing accuracy, hardware safety or real PLC execution.

## Expected I/O to operate this scene

| Point | Type | Owner | Initial value |
| --- | --- | --- | --- |
| `product_weighed` | `BOOL` | **PC** | `False` |
| `printer_ready` | `BOOL` | **PC** | `False` |
| `label_data_valid` | `BOOL` | **PC** | `False` |
| `print_request` | `BOOL` | **PLC** | `False` |
| `label_applied` | `BOOL` | **PLC** | `False` |
| `weight_kg` | `REAL` | **PC** | `0` |
| `product_present` | `BOOL` | **PC** | `False` |
| `print_complete` | `BOOL` | **PC** | `False` |
| `application_complete` | `BOOL` | **PC** | `False` |
| `apply_request` | `BOOL` | **PLC** | `False` |
| `label_text` | `STRING` | **PLC** | `` |
