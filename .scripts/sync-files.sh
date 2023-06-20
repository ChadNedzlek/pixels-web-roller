#!/usr/bin/env bash
TARGET_HOST=web.vaettir.net
echo uploading ../PixelsWeb/bin/Release/net7.0/publish/wwwroot to $TARGET_HOST
rsync -e 'ssh -i ~/.ssh/web.vaettir.net -o StrictHostKeyChecking=no -o UserKnownHostsFile=/dev/null' \
  -rzP \
  --delete \
  ../PixelsWeb/bin/Release/net7.0/publish/wwwroot/ \
  root@$TARGET_HOST:/var/www/web.vaettir.net
