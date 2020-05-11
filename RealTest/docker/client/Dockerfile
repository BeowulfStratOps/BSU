FROM mcr.microsoft.com/dotnet/core/sdk as build

RUN apt update && apt install -y wget --quiet zip

WORKDIR /storage

RUN mkdir main
RUN mkdir side
RUN mkdir steam

RUN wget --quiet "https://github.com/CBATeam/CBA_A3/releases/download/v3.13.0.191116/CBA_A3_v3.13.0.zip" -O out.zip
RUN unzip -q out.zip
RUN mv @CBA_A3 steam/@cba

RUN wget --quiet "https://github.com/CBATeam/CBA_A3/releases/download/v3.9.1.181229/CBA_A3_v3.9.1.zip" -O out.zip
RUN unzip -q out.zip
RUN mv @CBA_A3 main/@cba

RUN wget --quiet "https://github.com/acemod/ACE3/releases/download/v3.12.3/ace3_3.12.3.zip" -O out.zip
RUN unzip -q out.zip
RUN mv @ace main/@ace

RUN wget --quiet "https://github.com/IDI-Systems/acre2/releases/download/v2.7.2.1022/acre2_2.7.2.1022.zip" -O out.zip
RUN unzip -q out.zip
RUN mv @acre2 side/@acre2

RUN wget --quiet "https://github.com/acemod/ACEX/releases/download/v3.5.0-rc1/acex_3.5.0-rc1.zip" -O out.zip
RUN unzip -q out.zip
RUN mv @acex side/@acex

WORKDIR /build_script
COPY RealTest/docker/client/Corrupt.* ./
RUN dotnet build

WORKDIR /build_script/bin/Debug/netcoreapp3.1/
RUN dotnet Corrupt.dll /storage/main/@ace
RUN dotnet Corrupt.dll /storage/side/@acre2

WORKDIR /build
COPY . .

WORKDIR /build/RealTest
RUN dotnet build

RUN mkdir /client

WORKDIR /build/RealTest/bin/Debug/netcoreapp3.0

CMD dotnet RealTest.dll