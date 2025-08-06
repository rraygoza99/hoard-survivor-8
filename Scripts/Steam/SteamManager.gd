extends Node

signal lobby_created(lobby_id)
signal lobby_joined

func _ready():
	# Initialize Steam
	var steam_init_success = Steam.steamInit()
	if steam_init_success:
		print("Steam initialized")
		Steam.lobby_created.connect(_on_LobbyCreated)
		Steam.lobby_joined.connect(_on_LobbyJoined)
	else:
		printerr("Failed to initialize Steam")
func _process(delta):
	Steam.run_callbacks()

func create_lobby():
	Steam.createLobby(Steam.LOBBY_TYPE_FRIENDS_ONLY, 4)
	
func join_lobby(lobby_id_string):
	if not lobby_id_string.is_valid_int():
		printerr("Invalid Lobby ID")
		return
	var lobby_id = lobby_id_string.to_int()
	print("Attempting to join lobby")
	Steam.joinLobby(lobby_id)

func _on_LobbyCreated(was_successful, lobby_id):
	if was_successful:
		print("Lobby created")
		emit_signal("lobby_created", lobby_id)
	else:
		printerr("Failed to create lobby.")
		
func _on_LobbyJoined(lobby_id, permissions, locked, response):
	if response == 1:
		print("Successfully joined ", lobby_id)
		emit_signal("lobby_joined")
	else:
		printerr("Failed to join lobby")
