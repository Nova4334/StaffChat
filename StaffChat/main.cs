using Microsoft.Xna.Framework;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace StaffChatPlugin
{
	[ApiVersion(2,1)]
	public class StaffChat : TerrariaPlugin
	{
		public static bool[] Spying = new bool[255];
		public static bool[] InStaffChat = new bool[255];

		public static Config config = new Config();

		Color staffchatcolor = new Color(200, 50, 150);

		public override Version Version
		{
			get { return Assembly.GetExecutingAssembly().GetName().Version; }
		}
		public override string Author
		{
			get { return "Ancientgods"; }
		}
		public override string Name
		{
			get { return "StaffChat"; }
		}
		public override string Description
		{
			get { return "Allows staff to chat together in a group without other people seeing it."; }
		}

		public override void Initialize()
		{
			PlayerHooks.PlayerCommand += OnCommand;
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
			ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				PlayerHooks.PlayerCommand -= OnCommand;
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
			}
			base.Dispose(disposing);
		}

		public StaffChat(Main game)
			: base(game)
		{
			Order = 1;
		}

		private void OnInitialize(EventArgs args)
		{
			Commands.ChatCommands.Add(new Command(StaffChat_Chat, "s") { AllowServer = false });
			Commands.ChatCommands.Add(new Command(Permission.Invite, StaffChat_Kick, "skick"));
			Commands.ChatCommands.Add(new Command(Permission.Invite, StaffChat_Invite, "sinvite"));
			Commands.ChatCommands.Add(new Command(Permission.Invite, StaffChat_Clear, "sclear"));
			Commands.ChatCommands.Add(new Command(Permission.List, StaffChat_List, "slist"));
			Commands.ChatCommands.Add(new Command(Permission.List, ShowStaff, "staff"));
			Commands.ChatCommands.Add(new Command(Permission.SpyWhisper, SpyWhisper, "spywhisper") { AllowServer = false });

			if (!File.Exists(Config.SavePath))
				config.Save();
			else
				config = Config.Load();
		}

		private void OnCommand(PlayerCommandEventArgs args)
		{
			if (args.CommandName.Equals("w") || args.CommandName.Equals("whisper") || args.CommandName.Equals("reply") || args.CommandName.Equals("r") || args.CommandName.Equals("tell"))
			{
				foreach (var player in TShock.Players.Where(e => e != null && Spying[e.Index]))
					player.SendMessage($"{args.Player.Name}: {args.CommandText}", staffchatcolor);
			}
		}

		public void OnLeave(LeaveEventArgs e)
		{
			Spying[e.Who] = false;
			InStaffChat[e.Who] = false;
		}

		private void StaffChat_Chat(CommandArgs args)
		{
			if (args.Parameters.Count == 0)
			{
				args.Player.SendErrorMessage($"Invalid syntax! proper syntax: {TShock.Config.CommandSpecifier}s <message>");
				return;
			}

			if (!args.Player.HasPermission(Permission.Chat) && !InStaffChat[args.Player.Index])
			{
				args.Player.SendErrorMessage("You need to be invited to talk in the staffchat!");
				return;
			}

			foreach (TSPlayer ts in TShock.Players.Where(e => e != null && (e.HasPermission(Permission.Chat) || InStaffChat[e.Index])))
				ts.SendMessage($"{config.StaffChatPrefix}{(InStaffChat[args.Player.Index] ? " " + config.StaffChatGuestTag : "")} {args.Player.Name}: {string.Join(" ", args.Parameters)}", staffchatcolor);
		}

		private void StaffChat_Invite(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage($"Invalid syntax! Syntax: {TShock.Config.CommandSpecifier}sinvite <player>");
				return;
			}
			var foundplr = TSPlayer.FindByNameOrID(string.Join(" ", args.Parameters));
			if (foundplr.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid player!");
				return;
			}
			if (foundplr.Count > 1)
			{
				TShock.Players[args.Player.Index].SendMultipleMatchError(foundplr.Select(e => e.Name));
				return;
			}
			var plr = foundplr[0];

			if (plr.HasPermission(Permission.Chat) || InStaffChat[plr.Index])
			{
				args.Player.SendErrorMessage("This player is already in the staffchat!");
				return;
			}

			InStaffChat[plr.Index] = true;
			plr.SendInfoMessage($"You have been invited into the staffchat, type \"{TShock.Config.CommandSpecifier}s <message>\" to talk.");

			foreach (TSPlayer ts in TShock.Players.Where(e => e != null && e.Index != plr.Index && e.HasPermission(Permission.Chat)))
				ts.SendInfoMessage(plr.Name + " has been invited into the staffchat.");
		}

		private void StaffChat_Kick(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage($"Invalid syntax! Syntax: {TShock.Config.CommandSpecifier}skick <player>");
				return;
			}
			var foundplr = TSPlayer.FindByNameOrID(string.Join(" ", args.Parameters));
			if (foundplr.Count == 0)
			{
				args.Player.SendErrorMessage("Invalid player!");
				return;
			}
			if (foundplr.Count > 1)
			{
                TShock.Players[args.Player.Index].SendMultipleMatchError(foundplr.Select(e => e.Name));
				return;
			}
			var plr = foundplr[0];

			if (!InStaffChat[plr.Index] || !plr.HasPermission(Permission.Chat))
			{
				args.Player.SendErrorMessage("This player is not in the staffchat!");
				return;
			}
			if (plr.HasPermission(Permission.Chat))
			{
				args.Player.SendErrorMessage("You can't kick another staff member from the staffchat!");
				return;
			}

			InStaffChat[plr.Index] = false;
			plr.SendInfoMessage("You have been removed from staffchat!");

			foreach (TSPlayer ts in TShock.Players.Where(e => e != null && (e.HasPermission(Permission.Chat) || InStaffChat[e.Index])))
					ts.SendInfoMessage(plr.Name + " has been removed from the staffchat.");
		}

		private void StaffChat_Clear(CommandArgs args)
		{
			foreach (TSPlayer ts in TShock.Players.Where(e => e != null && (InStaffChat[e.Index] || e.HasPermission(Permission.Chat))))
			{
				if (InStaffChat[ts.Index])
				{
					InStaffChat[ts.Index] = false;
					ts.SendErrorMessage("You have been removed from the staffchat!");
				}
				else
					ts.SendInfoMessage("All guests have been removed from the staffchat!");
			}
		}

		private void StaffChat_List(CommandArgs args)
		{
			string players = string.Join(", ", TShock.Players.Where(e => e != null && InStaffChat[e.Index]).Select(e => e.Name));

			if (!string.IsNullOrWhiteSpace(players))
				args.Player.SendInfoMessage($"Guests in the staffchat: {players}");
			else
				args.Player.SendInfoMessage("There are no guests in the staffchat!");
		}

		public void ShowStaff(CommandArgs args)
		{
			var staff = from staffmember in TShock.Players where staffmember != null && staffmember.HasPermission(Permission.StaffMember) orderby staffmember.Group.Name select staffmember;
			args.Player.SendInfoMessage("~ Currently online staffmembers ~");
			foreach (TSPlayer ts in staff)
			{
				if (ts == null)
					continue;

				Color groupcolor = new Color(ts.Group.R, ts.Group.G, ts.Group.B);
				args.Player.SendMessage(string.Format($"{ts.Group.Prefix}{ts.Name}"), groupcolor);
			}
		}

		private void SpyWhisper(CommandArgs args)
		{
			if (args.Parameters.Count == 1 && args.Parameters[0] == "-l")
			{
				args.Player.SendInfoMessage("Currently spying on whispers:");
				foreach (TSPlayer ts in TShock.Players.Where(e => e != null && Spying[e.Index]))
					args.Player.SendInfoMessage(ts.Name);
				args.Player.SendInfoMessage(new string('-', 30));
				return;
			}
			Spying[args.Player.Index] = !Spying[args.Player.Index];
			args.Player.SendInfoMessage($"You are {(Spying[args.Player.Index] ? "now" : "not")} spying on whispers");
		}
	}
}

