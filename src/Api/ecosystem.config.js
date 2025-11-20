module.exports = {
  apps: [
    {
      name: "apiGuardian",
      script: "dotnet",
      args: "/bin/Release/net9.0/publish/MiProyecto.dll",
      cwd: "/var/www/apiGuardian",
      watch: false,
      autorestart: true,
      max_memory_restart: "500M"
    }
  ]
}