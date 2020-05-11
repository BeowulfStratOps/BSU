FROM mcr.microsoft.com/dotnet/core/sdk as build

RUN apt update && apt install -y wget --quiet zip


RUN mkdir /serve
WORKDIR /data

RUN wget --quiet "https://github.com/CBATeam/CBA_A3/releases/download/v3.13.0.191116/CBA_A3_v3.13.0.zip" -O out.zip
RUN unzip -q out.zip
RUN ls -alh
RUN mv @CBA_A3 @cba_v2

RUN wget --quiet "https://github.com/CBATeam/CBA_A3/releases/download/v3.9.1.181229/CBA_A3_v3.9.1.zip" -O out.zip
RUN unzip -q out.zip
RUN mv @CBA_A3 @cba_v1

RUN wget --quiet "https://github.com/acemod/ACE3/releases/download/v3.12.3/ace3_3.12.3.zip" -O out.zip
RUN unzip -q out.zip
RUN mv @ace @ace_v1

RUN wget --quiet "https://github.com/acemod/ACE3/releases/download/v3.13.0-rc1/ace3_3.13.0-rc1.zip" -O out.zip
RUN unzip -q out.zip
RUN mv @ace @ace_v2

RUN wget --quiet "https://github.com/IDI-Systems/acre2/releases/download/v2.7.2.1022/acre2_2.7.2.1022.zip" -O out.zip
RUN unzip -q out.zip
RUN mv @acre2 @acre2_v2

RUN wget --quiet "https://github.com/IDI-Systems/acre2/releases/download/v2.5.1.980/acre2_2.5.1.980.zip" -O out.zip
RUN unzip -q out.zip
RUN mv @acre2 @acre2_v1

RUN wget --quiet "https://github.com/acemod/ACEX/releases/download/v3.5.0-rc1/acex_3.5.0-rc1.zip" -O out.zip
RUN unzip -q out.zip

WORKDIR /build
COPY BSU.Hashes ./BSU.Hashes
COPY BSU.BSO ./BSU.BSO
COPY BSU.Server ./BSU.Server
COPY BSU.CoreCommon ./BSU.CoreCommon

WORKDIR /build/BSU.Server
RUN dotnet build

COPY RealTest/docker/server/repo*.ini /data/

WORKDIR /build/BSU.Server/bin/Debug/netcoreapp3.0
RUN dotnet BSU.Server.dll /data/repo1.ini
RUN dotnet BSU.Server.dll /data/repo2.ini
RUN dotnet BSU.Server.dll /data/repo3.ini

FROM nginx

COPY --from=build /serve /usr/share/nginx/html

RUN rm /usr/share/nginx/html/index.html

COPY RealTest/docker/server/default.conf /etc/nginx/conf.d/default.conf