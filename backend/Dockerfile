# Render shu Dockerfile orqali build qiladi.
# Render sozlamasi: Root Directory = backend, Dockerfile Path = ./Dockerfile
# (Clean Architecture'dan keyin backend 4 ta loyihadan iborat — build konteksti
# butun backend papkasi bo'lishi kerak).
#
# Versiya eslatmasi: image tegi (masalan "10.0") lokal `dotnet --version`
# bilan mos kelishi shart (hozir net10.0). Kelajakda lokal SDK yangilansa,
# shu ikkita qatordagi raqamni ham yangilashni unutmang.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Avval faqat loyiha fayllari — paketlar keshlanadi (kod o'zgarsa ham restore qayta ishlamaydi).
COPY SpeakingCoach.Domain/SpeakingCoach.Domain.csproj SpeakingCoach.Domain/
COPY SpeakingCoach.Application/SpeakingCoach.Application.csproj SpeakingCoach.Application/
COPY SpeakingCoach.Infrastructure/SpeakingCoach.Infrastructure.csproj SpeakingCoach.Infrastructure/
COPY SpeakingCoach.Api/SpeakingCoach.Api.csproj SpeakingCoach.Api/
RUN dotnet restore SpeakingCoach.Api/SpeakingCoach.Api.csproj
COPY SpeakingCoach.Domain/ SpeakingCoach.Domain/
COPY SpeakingCoach.Application/ SpeakingCoach.Application/
COPY SpeakingCoach.Infrastructure/ SpeakingCoach.Infrastructure/
COPY SpeakingCoach.Api/ SpeakingCoach.Api/
RUN dotnet publish SpeakingCoach.Api/SpeakingCoach.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
# Xavfsizlik: ilova root emas, oddiy foydalanuvchi ("app", obrazda tayyor)
# nomidan ishlaydi — diskka yozmaydi, PORT esa 1024 dan katta (Render: 10000).
USER $APP_UID
ENTRYPOINT ["dotnet", "SpeakingCoach.Api.dll"]
