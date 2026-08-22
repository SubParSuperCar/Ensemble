namespace Root.SessionManager.Api;

public enum SessionMode : byte
{
	Inactive, // No session is running.
	SinglePlayer, // A single player session is running: completely offline.
	MultiPlayer // A multi-player session is running: playing with others/friends.
}
