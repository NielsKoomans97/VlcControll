# VlcControll
Discord bot for control over the VLC Lua web interface

## If you want it working for your setup, you need to follow the following steps:
> ### **THIS IS IMPORTANT!**
> **You can't use my personal token I got from the Discord Developer Portal. You need to create a Bot yourself, and paste the token you get from there into an extension-less **token** file, and put it in the directory where the executable is.**
> **[Discord Developer Portal](https://discord.com/developers)**

1. In VLC Player, go to **Preferences**. Make sure that under **Viewing options**, the radio for **Everything** is selected.
2. In the menu on the left, go to **Interface** -> **Main interfaces**.
3. From here, on the view on the right, enable the **Lua interpreter** and the **Web** checkbox. Make sure to set a password at **Interface** -> **Main interfaces** -> **Lua**
4. Make sure to save the changed settings by clicking on **Save**.
5. In **program.cs**, you change the IP adres in the variables **playlist_url** and **status_url**, to the IP adres of the computer you're running VLC player on.
6. In **program.cs**, specifically in `var byteArray = Encoding.ASCII.GetBytes(":F!nley19g7");
        httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));`, Change the value after the ':' character in the GetBytes method to the password you set in your VLC preferences.
