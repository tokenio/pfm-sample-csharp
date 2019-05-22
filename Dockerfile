FROM mono:5.18

# Install xsp4
RUN apt-get update \
    && apt-get install mono-xsp4 -y \
    && mkdir /usr/src/sample

WORKDIR /usr/src/sample

COPY ./packages.config .
COPY ./pfm-sample-csharp.* ./

RUN nuget restore

COPY . .

RUN msbuild

ENTRYPOINT ["xsp4", "--nonstop"]

CMD ["--address=localhost",  "--port=3000"]
