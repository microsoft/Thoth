window.sendToTeams = (filename, url) => {
    microsoftTeams.sharing.shareWebContent({
        content: [
            {
                type: 'URL',
                url: url,
                message: `Check out this document I found using Microsoft Thoth: ${filename}.`,
                preview: true
            }
        ]
    });
}