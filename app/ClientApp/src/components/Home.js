import axios from 'axios';
import React, { useState } from 'react';

export function Home() {
    const [error, setError] = useState(null);
    const [replayData, setReplayData] = useState({});
    const [analysisUrl, setAnalysisUrl] = useState(null);
    const [pending, setPending] = useState(false);

    const onFileChange = event => {
        const selectedFile = event.target.files[0];

        setPending(true);
        setError(null);

        const formData = new FormData();

        formData.append(
            "replay",
            selectedFile,
            selectedFile.name
        );

        axios.post("replay", formData).then(response => {
            const replayData = response.data;
            console.log(replayData);
            setReplayData(replayData.game);
            setAnalysisUrl(replayData.analysisUrl);
            setPending(false);
        }).catch(err => {
            console.error(err);
            setError("There was an error uploading your replay: " + err);
            setPending(false);
        });
    };

    return (
        <div>
            <h1>Welcome, welcome!</h1>
            <p>Curious about which platforms your opponents were on? Upload your replay and find out!</p>
            {replayData.winningDisplayNames && replayData.winningDisplayNames.length > 0 &&
                <div><h1>#1 Victory Royale:</h1>  <p>{replayData.winningDisplayNames.join(", ")}</p></div>
            }
            {replayData.platformStatistics &&
                <div>
                    <h2>Platform statistics for your game:</h2>
                    <div>Windows: {replayData.platformStatistics["win"] || 0}</div>
                    <div>PlayStation: {replayData.platformStatistics["psn"] || 0}</div>
                    <div>Mac: {replayData.platformStatistics["mac"] || 0}</div>
                    <div>XBox: {replayData.platformStatistics["xbl"] || 0}</div>
                    <div>Nintendo Switch: {replayData.platformStatistics["swt"] || 0}</div>
                    <div>iPhone or iPad: {replayData.platformStatistics["ios"] || 0}</div>
                    <div>Android: {replayData.platformStatistics["and"] || 0}</div>
                </div>
            }
            {replayData.botCount &&
                <div><p>Bot count: <b>{replayData.botCount}</b></p></div>
            }
            {replayData.realPlayerCount &&
                <div><p>Real player count: <b>{replayData.realPlayerCount}</b></p></div>
            }
            {replayData.playerCount &&
                <div><p>Total player count: <b>{replayData.playerCount}</b></p></div>
            }
            {replayData.eliminations &&
                <div>
                    <h2>Eliminations</h2>
                    {replayData.eliminations.map((elimination, index) => <div key={index}>{elimination.eliminatedBy.name} ({elimination.eliminatedBy.platform}) - {elimination.eliminated.name} ({elimination.eliminated.platform})</div>)}
                </div>
            }
            {pending &&
                <div><p>Uploading, please wait ...</p></div>
            }
            {analysisUrl && <div><h2>Share this analysis</h2><a href={analysisUrl}>Link to replay analysis (valid for 7 days)</a></div>}
            {error && <div><p>{error}</p></div>}
            {pending ||
                <div>
                    <h2>Upload replay to see statistics:</h2>
                    <div>
                        <div className="input-group">
                            <div className="custom-file">
                                <input type="file" className="custom-file-input" id="inputGroupFile01"
                                    aria-describedby="inputGroupFileAddon01" onChange={onFileChange} accept=".replay" />
                                <label className="custom-file-label" htmlFor="inputGroupFile01">Choose file</label>
                            </div>
                        </div>
                    </div>
                    <p></p>
                </div>
            }
            <h2>Where can I find the replay files?</h2>
            <p>To find the folder where your replays are stored, open Fortnite, go to the Career tab and then Replays. Click on "Open replay folder" to see where they are stored. On Windows this folder is usually <b>%localappdata%\FortniteGame\Saved\Demos</b> or <b>C:\Users\[your&nbsp;username]\Local\FortniteGame\Saved\Demos</b>.</p>
        </div >
    );
}
