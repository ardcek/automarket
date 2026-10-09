# Rota Rent a Car — Docker imajı (Render.com ve her Docker sunucusunda çalışır)
#
# Lokalde denemek için (Docker kuruluysa, AracKiralama klasöründe):
#   docker build -t rota .
#   docker run -p 8080:8080 rota          → http://localhost:8080

# ---------- 1. Aşama: derleme ----------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Önce sadece proje dosyası kopyalanır → paketler değişmedikçe "restore" adımı önbellekten gelir (hızlı build).
COPY src/AracKiralama.Web/AracKiralama.Web.csproj src/AracKiralama.Web/
# PublishReadyToRun restore sırasında da verilmeli; yoksa Linux için gereken derleyici paketi inmez (NETSDK1094).
RUN dotnet restore src/AracKiralama.Web/AracKiralama.Web.csproj -r linux-x64 -p:PublishReadyToRun=true

COPY src/ src/
# ReadyToRun: kod önceden makine koduna derlenir → zayıf (ücretsiz) sunucuda uygulama daha hızlı açılır.
RUN dotnet publish src/AracKiralama.Web/AracKiralama.Web.csproj -c Release -r linux-x64 --self-contained false \
    -p:PublishReadyToRun=true -o /app/publish --no-restore

# ---------- 2. Aşama: çalışma imajı (sadece çalışma zamanı, SDK yok → küçük imaj) ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

# tzdata: Türkiye saati için · libfontconfig1: PDF sözleşme (QuestPDF) için
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080 \
    TZ=Europe/Istanbul \
    DATA_DIR=/app/data

WORKDIR /app
COPY --from=build /app/publish .

# Veritabanı, yüklenen görseller ve oturum anahtarları bu klasöre yazılır.
RUN mkdir -p /app/data && chown -R $APP_UID /app/data

# Güvenlik: uygulama root olarak değil, imajdaki sınırlı "app" kullanıcısıyla çalışır.
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "AracKiralama.Web.dll"]
