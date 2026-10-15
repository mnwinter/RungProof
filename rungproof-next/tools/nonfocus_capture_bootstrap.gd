extends SceneTree

# Development capture only. The launcher starts a transparent 1px no-focus
# window. Move it onto an actual secondary display before revealing the app.
func _initialize() -> void:
	var window := root
	var native_no_focus := DisplayServer.window_get_flag(DisplayServer.WINDOW_FLAG_NO_FOCUS)
	print("NONFOCUS_CAPTURE_STARTUP setting=", ProjectSettings.get_setting("display/window/size/no_focus"),
		" nativeFlag=", native_no_focus, " windowProxy=", window.unfocusable)
	if not native_no_focus:
		push_error("NONFOCUS_CAPTURE_FAIL startup no-focus flag missing")
		quit(2)
		return
	# SceneTree's Window wrapper can retain its constructor default during
	# _initialize even when the native display already has the startup flag.
	window.unfocusable = true
	# SceneTree finishes synchronizing its root Window after _initialize. Reveal
	# only after that initialization, otherwise its tiny startup size overwrites
	# our requested viewport and produces a 64px placeholder capture.
	call_deferred("_reveal_secondary")

func _reveal_secondary() -> void:
	var window := root
	if not DisplayServer.window_get_flag(DisplayServer.WINDOW_FLAG_NO_FOCUS):
		push_error("NONFOCUS_CAPTURE_FAIL no-focus flag changed during initialization")
		quit(2)
		return
	window.mouse_passthrough = true
	var primary := DisplayServer.get_primary_screen()
	var secondary := -1
	for index in range(DisplayServer.get_screen_count()):
		if index != primary:
			secondary = index
			break
	if secondary < 0:
		push_error("NONFOCUS_CAPTURE_FAIL no secondary display")
		quit(2)
		return
	var area := DisplayServer.screen_get_usable_rect(secondary)
	var target := Vector2i(mini(1600, area.size.x - 40), mini(900, area.size.y - 60))
	if target.x < 1200 or target.y < 675:
		push_error("NONFOCUS_CAPTURE_FAIL secondary display too small for supported viewport")
		quit(2)
		return
	window.current_screen = secondary
	window.position = area.position + (area.size - target) / 2
	window.borderless = false
	window.transparent = false
	window.transparent_bg = false
	# Apply client size after adding window decorations; reversing this order
	# silently shrinks the rendered viewport by the non-client border/title bar.
	for argument in OS.get_cmdline_user_args():
		if argument.begins_with("--capture-size="):
			var parts := argument.trim_prefix("--capture-size=").split("x")
			if parts.size() != 2 or not parts[0].is_valid_int() or not parts[1].is_valid_int():
				push_error("NONFOCUS_CAPTURE_FAIL invalid requested viewport")
				quit(2)
				return
			var requested := Vector2i(parts[0].to_int(), parts[1].to_int())
			if requested.x < 1200 or requested.y < 675 or requested.x > target.x or requested.y > target.y:
				push_error("NONFOCUS_CAPTURE_FAIL requested viewport outside secondary display")
				quit(2)
				return
			target = requested
	window.size = target
	if not DisplayServer.window_get_flag(DisplayServer.WINDOW_FLAG_NO_FOCUS):
		push_error("NONFOCUS_CAPTURE_FAIL reveal changed no-focus flag")
		quit(2)
		return
	print("NONFOCUS_CAPTURE_READY primary=", primary, " secondary=", secondary,
		" size=", target, " noFocus=", window.unfocusable, " mousePassthrough=", window.mouse_passthrough)
	var error := change_scene_to_file("res://scenes/app/main.tscn")
	if error != OK:
		push_error("NONFOCUS_CAPTURE_FAIL main scene load error " + str(error))
		quit(2)
