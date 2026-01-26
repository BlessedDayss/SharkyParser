import { app } from 'electron'
import { spawn } from 'child_process'
import * as fs from 'fs'
import * as path from 'path'

export class BackendService {
    private readonly sharkyPath: string

    constructor() {
        this.sharkyPath = this.findSharky()
    }

    public isReady(): boolean {
        return fs.existsSync(this.sharkyPath)
    }

    public async parseLog(filePath: string, logType: string = 'Installation'): Promise<any> {
        return new Promise((resolve, reject) => {
            let output = ''
            const proc = spawn(this.sharkyPath, ['parse', filePath, '--type', logType, '--embedded'])
            proc.stdout.on('data', (data) => output += data.toString())
            proc.stderr.on('data', (data) => console.error('CLI stderr:', data.toString()))
            proc.on('close', (code) => {
                if (code === 0) resolve(this.parseEmbeddedOutput(output))
                else reject(new Error(`Exit code ${code}`))
            })
        })
    }

    public async analyzeLog(filePath: string, logType: string = 'Installation'): Promise<any> {
        return new Promise((resolve, reject) => {
            let output = ''
            const proc = spawn(this.sharkyPath, ['analyze', filePath, '--type', logType, '--embedded'])
            proc.stdout.on('data', (data) => output += data.toString())
            proc.stderr.on('data', (data) => console.error('CLI stderr:', data.toString()))
            proc.on('close', (code) => {
                if (code === 0) {
                    const line = output.trim().split('\n').find(l => l.startsWith('ANALYSIS|'))
                    if (line) {
                        const p = line.split('|')
                        resolve({
                            total: parseInt(p[1]),
                            errors: parseInt(p[2]),
                            warnings: parseInt(p[3]),
                            info: parseInt(p[4]),
                            debug: parseInt(p[5]),
                            status: p[6],
                            extendedData: p[7] || ''
                        })
                    } else {
                        reject(new Error("No analysis data found"))
                    }
                } else reject(new Error(`Exit code ${code}`))
            })
        })
    }

    private findSharky(): string {
        const isDev = !app.isPackaged
        const appPath = app.getAppPath()
        const exeName = process.platform === 'win32' ? 'SharkyParser.Cli.exe' : 'SharkyParser.Cli'

        if (isDev) {
            const paths = [
                path.join(appPath, 'dotnet', exeName),
                path.join(appPath, '..', 'SharkyParser.Cli', 'bin', 'Debug', 'net8.0', exeName)
            ]
            for (const p of paths) if (fs.existsSync(p)) return p
        }
        return path.join(process.resourcesPath, 'dotnet', exeName)
    }

    private parseEmbeddedOutput(output: string) {
        const lines = output.trim().split('\n').filter(line => line.includes('|'))
        const entries: any[] = []
        let statistics = { total: 0, errors: 0, warnings: 0, info: 0, debug: 0 }

        for (const line of lines) {
            if (line.startsWith('STATS|')) {
                const parts = line.split('|')
                statistics = {
                    total: parseInt(parts[1]) || 0,
                    errors: parseInt(parts[2]) || 0,
                    warnings: parseInt(parts[3]) || 0,
                    info: parseInt(parts[4]) || 0,
                    debug: parseInt(parts[5]) || 0
                }
            } else if (line.startsWith('ENTRY|')) {
                const parts: string[] = []
                let current = ''
                let escaped = false
                for (let i = 6; i < line.length; i++) {
                    if (line[i] === '\\' && !escaped) { escaped = true; continue; }
                    if (line[i] === '|' && !escaped) { parts.push(current); current = ''; continue; }
                    current += line[i]; escaped = false;
                }
                parts.push(current)
                entries.push({
                    timestamp: parts[0] || '',
                    level: parts[1] || 'INFO',
                    message: parts[2] || '',
                    source: parts[3] || '',
                    stackTrace: parts[4] || '',
                    lineNumber: parts[5] || '',
                    filePath: parts[6] || '',
                    rawData: parts[7] || ''
                })
            }
        }
        return { entries, statistics }
    }
}
