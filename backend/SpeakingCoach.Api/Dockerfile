# Render Dockerfile'ni ko'rib, avtomatik shu orqali build qiladi —
# .NET uchun bu buildpack'lardan ko'ra ishonchliroq yo'l.
#
# Versiya eslatmasi: image tegi (masalan "10.0") lokal `dotnet --version`
# bilan mos kelishi shart (hozir net10.0). Kelajakda lokal SDK yangilansa,
# shu ikkita qatordagi raqamni ham yangilashni unutmang.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY *.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
# Xavfsizlik: ilova root emas, oddiy foydalanuvchi ("app", obrazda tayyor)
# nomidan ishlaydi — diskka yozmaydi, PORT esa 1024 dan katta (Render: 10000).
USER $APP_UID
ENTRYPOINT ["dotnet", "SpeakingCoach.Api.dll"]
