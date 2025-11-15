docker run -d `
	--name firebird-db `
	--env-file ".env" `
	-v ./data:/var/lib/firebird/data `
	 firebirdsql/firebird