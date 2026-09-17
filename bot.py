from __future__ import annotations

import logging
import os

import discord
from discord import app_commands
from dotenv import load_dotenv

from account_store import AccountStore

load_dotenv()
logging.basicConfig(level=logging.INFO)
ADMIN_USER_ID = 533641917766828032


class AccountBot(discord.Client):
    def __init__(self, store: AccountStore) -> None:
        intents = discord.Intents.none()
        super().__init__(intents=intents)
        self.store = store
        self.tree = app_commands.CommandTree(self)

    async def setup_hook(self) -> None:
        guild_id = os.getenv("DISCORD_GUILD_ID")
        if guild_id:
            guild = discord.Object(id=int(guild_id))
            commands = list(self.tree.get_commands())
            self.tree.clear_commands(guild=None)
            await self.tree.sync()
            for command in commands:
                self.tree.add_command(command, guild=guild, override=True)
            await self.tree.sync(guild=guild)
            logging.info("Synced commands to guild %s", guild_id)
        else:
            await self.tree.sync()


store = AccountStore(os.getenv("DATABASE_PATH", "accounts.sqlite3"))
bot = AccountBot(store)
if store.database_url:
    logging.info("Using shared PostgreSQL account database")
else:
    logging.info("Using SQLite account database at %s", store.database_path.resolve())


@bot.event
async def on_ready() -> None:
    logging.info("Bot is online as %s", bot.user)


@bot.tree.command(name="create-account", description="Create your download account")
async def create_account(interaction: discord.Interaction) -> None:
    account = store.create_account(interaction.user.id)
    if account is None:
        logging.info("Account creation skipped for Discord user %s", interaction.user.id)
        await interaction.response.send_message(
            "You already have an account, or the account could not be created.",
            ephemeral=True,
        )
        return

    logging.info("Created account for Discord user %s", interaction.user.id)

    try:
        await interaction.user.send(
            "Your account was created. Keep these credentials private.\n"
            f"Username: `{account.username}`\n"
            f"Password: `{account.password}`"
        )
        await interaction.response.send_message(
            "Your credentials were sent in a private message.", ephemeral=True
        )
    except discord.Forbidden:
        await interaction.response.send_message(
            "I could not DM you. Enable direct messages for this server and try again.",
            ephemeral=True,
        )


@bot.tree.command(name="remove-account", description="Remove a user's download account")
@app_commands.describe(user="The Discord user whose account should be removed")
async def remove_account(interaction: discord.Interaction, user: discord.User) -> None:
    if interaction.user.id != ADMIN_USER_ID:
        await interaction.response.send_message(
            "You are not authorized to remove accounts.", ephemeral=True
        )
        return

    removed = store.remove_account(user.id)
    message = (
        f"Removed the download account for {user.mention}."
        if removed
        else f"No download account exists for {user.mention}."
    )
    await interaction.response.send_message(message, ephemeral=True)


def main() -> None:
    token = os.getenv("DISCORD_TOKEN")
    if not token:
        raise RuntimeError(
            "DISCORD_TOKEN is missing. Copy .env.example to .env and set the bot token."
        )
    bot.run(token)


if __name__ == "__main__":
    main()