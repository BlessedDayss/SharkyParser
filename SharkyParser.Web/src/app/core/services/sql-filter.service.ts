import { Injectable } from '@angular/core';
import { LogEntry } from '../models/log-entry';

// ── Token types ───────────────────────────────────────────────────────────────
type TokType =
  | 'IDENT' | 'STRING' | 'NUMBER'
  | 'EQ' | 'NEQ' | 'GT' | 'GTE' | 'LT' | 'LTE'
  | 'LIKE' | 'NOT' | 'AND' | 'OR'
  | 'LPAREN' | 'RPAREN'
  | 'EOF';

interface Token { type: TokType; value: string; }

// ── AST nodes ─────────────────────────────────────────────────────────────────
type Expr =
  | { kind: 'compare'; field: string; op: string; value: string }
  | { kind: 'like';    field: string; pattern: string; negate: boolean }
  | { kind: 'and';     left: Expr; right: Expr }
  | { kind: 'or';      left: Expr; right: Expr }
  | { kind: 'not';     expr: Expr };

export interface SqlFilterResult {
  ok: boolean;
  error?: string;
  matched?: number;
}

@Injectable({ providedIn: 'root' })
export class SqlFilterService {

  // ── Public API ─────────────────────────────────────────────────────────────

  /** Validate query and return parse result. */
  validate(query: string): { ok: boolean; error?: string } {
    if (!query.trim()) return { ok: true };
    try {
      this.parse(this.tokenize(query));
      return { ok: true };
    } catch (e: any) {
      return { ok: false, error: e.message };
    }
  }

  /** Filter entries by SQL WHERE query. Returns original array if query is empty/invalid. */
  filter(entries: LogEntry[], query: string): LogEntry[] {
    if (!query.trim()) return entries;
    try {
      const ast = this.parse(this.tokenize(query));
      return entries.filter(e => this.evalExpr(ast, e));
    } catch {
      return entries;
    }
  }

  // ── Tokenizer ──────────────────────────────────────────────────────────────

  private tokenize(input: string): Token[] {
    const tokens: Token[] = [];
    let i = 0;

    while (i < input.length) {
      // Skip whitespace
      if (/\s/.test(input[i])) { i++; continue; }

      // Two-char operators
      if (input[i] === '>' && input[i + 1] === '=') { tokens.push({ type: 'GTE', value: '>=' }); i += 2; continue; }
      if (input[i] === '<' && input[i + 1] === '=') { tokens.push({ type: 'LTE', value: '<=' }); i += 2; continue; }
      if (input[i] === '!' && input[i + 1] === '=') { tokens.push({ type: 'NEQ', value: '!=' }); i += 2; continue; }
      if (input[i] === '<' && input[i + 1] === '>') { tokens.push({ type: 'NEQ', value: '<>' }); i += 2; continue; }

      // Single-char operators
      if (input[i] === '=')  { tokens.push({ type: 'EQ',     value: '=' });  i++; continue; }
      if (input[i] === '>')  { tokens.push({ type: 'GT',     value: '>' });  i++; continue; }
      if (input[i] === '<')  { tokens.push({ type: 'LT',     value: '<' });  i++; continue; }
      if (input[i] === '(')  { tokens.push({ type: 'LPAREN', value: '(' });  i++; continue; }
      if (input[i] === ')')  { tokens.push({ type: 'RPAREN', value: ')' });  i++; continue; }

      // Quoted string
      if (input[i] === "'" || input[i] === '"') {
        const q = input[i++];
        let s = '';
        while (i < input.length && input[i] !== q) {
          if (input[i] === '\\') i++;
          s += input[i++];
        }
        i++; // closing quote
        tokens.push({ type: 'STRING', value: s });
        continue;
      }

      // Number
      if (/[0-9]/.test(input[i])) {
        let n = '';
        while (i < input.length && /[0-9.]/.test(input[i])) n += input[i++];
        tokens.push({ type: 'NUMBER', value: n });
        continue;
      }

      // Identifier / keyword (supports hyphens and parens in IIS field names like cs(User-Agent))
      if (/[a-zA-Z_\-]/.test(input[i])) {
        let id = '';
        while (i < input.length && /[a-zA-Z0-9_\-\(\)]/.test(input[i])) id += input[i++];
        const upper = id.toUpperCase();
        if (upper === 'AND')  { tokens.push({ type: 'AND',  value: id }); continue; }
        if (upper === 'OR')   { tokens.push({ type: 'OR',   value: id }); continue; }
        if (upper === 'NOT')  { tokens.push({ type: 'NOT',  value: id }); continue; }
        if (upper === 'LIKE') { tokens.push({ type: 'LIKE', value: id }); continue; }
        tokens.push({ type: 'IDENT', value: id });
        continue;
      }

      throw new Error(`Unexpected character: '${input[i]}'`);
    }

    tokens.push({ type: 'EOF', value: '' });
    return tokens;
  }

  // ── Parser (recursive descent) ─────────────────────────────────────────────

  private parse(tokens: Token[]): Expr {
    let pos = 0;

    const peek  = () => tokens[pos];
    const eat   = (t?: TokType) => {
      const tok = tokens[pos++];
      if (t && tok.type !== t) throw new Error(`Expected ${t}, got ${tok.type} ('${tok.value}')`);
      return tok;
    };

    // expr → term (OR term)*
    const parseExpr = (): Expr => {
      let left = parseTerm();
      while (peek().type === 'OR') {
        eat('OR');
        left = { kind: 'or', left, right: parseTerm() };
      }
      return left;
    };

    // term → factor (AND factor)*
    const parseTerm = (): Expr => {
      let left = parseFactor();
      while (peek().type === 'AND') {
        eat('AND');
        left = { kind: 'and', left, right: parseFactor() };
      }
      return left;
    };

    // factor → NOT factor | '(' expr ')' | comparison
    const parseFactor = (): Expr => {
      if (peek().type === 'NOT') {
        eat('NOT');
        return { kind: 'not', expr: parseFactor() };
      }
      if (peek().type === 'LPAREN') {
        eat('LPAREN');
        const e = parseExpr();
        eat('RPAREN');
        return e;
      }
      return parseComparison();
    };

    // comparison → IDENT op value | IDENT [NOT] LIKE value
    const parseComparison = (): Expr => {
      const field = eat('IDENT').value;

      // NOT LIKE
      if (peek().type === 'NOT') {
        eat('NOT');
        eat('LIKE');
        const val = parseValue();
        return { kind: 'like', field, pattern: val, negate: true };
      }

      if (peek().type === 'LIKE') {
        eat('LIKE');
        const val = parseValue();
        return { kind: 'like', field, pattern: val, negate: false };
      }

      const opTok = tokens[pos++];
      if (!['EQ','NEQ','GT','GTE','LT','LTE'].includes(opTok.type))
        throw new Error(`Expected operator after field '${field}', got '${opTok.value}'`);

      const val = parseValue();
      return { kind: 'compare', field, op: opTok.value, value: val };
    };

    const parseValue = (): string => {
      const tok = tokens[pos++];
      if (tok.type === 'STRING' || tok.type === 'NUMBER' || tok.type === 'IDENT'
          || tok.type === 'AND' || tok.type === 'OR' || tok.type === 'NOT' || tok.type === 'LIKE')
        return tok.value;
      throw new Error(`Expected value, got '${tok.value}'`);
    };

    const ast = parseExpr();
    if (peek().type !== 'EOF') throw new Error(`Unexpected token: '${peek().value}'`);
    return ast;
  }

  // ── Evaluator ──────────────────────────────────────────────────────────────

  private evalExpr(expr: Expr, entry: LogEntry): boolean {
    switch (expr.kind) {
      case 'and':     return this.evalExpr(expr.left, entry) && this.evalExpr(expr.right, entry);
      case 'or':      return this.evalExpr(expr.left, entry) || this.evalExpr(expr.right, entry);
      case 'not':     return !this.evalExpr(expr.expr, entry);
      case 'like':    return this.evalLike(expr.field, expr.pattern, expr.negate, entry);
      case 'compare': return this.evalCompare(expr.field, expr.op, expr.value, entry);
    }
  }

  // ── Field alias map (display name → IIS field name) ───────────────────────
  private static readonly ALIASES: Record<string, string> = {
    // Predefined
    'timestamp':       'timestamp',
    'time':            'timestamp',
    'level':           'level',
    'severity':        'level',
    'message':         'message',
    'uri':             'message',

    // IIS display names (from UI headers)
    'status':          'sc-status',
    'statuscode':      'sc-status',
    'httpstatus':      'sc-status',
    'substatus':       'sc-substatus',
    'substatuscode':   'sc-substatus',
    'win32':           'sc-win32-status',
    'win32status':     'sc-win32-status',
    'method':          'cs-method',
    'httpmethod':      'cs-method',
    'verb':            'cs-method',
    'query':           'cs-uri-query',
    'querystring':     'cs-uri-query',
    'querystr':        'cs-uri-query',
    'port':            's-port',
    'serverport':      's-port',
    'serverip':        's-ip',
    'server':          's-ip',
    'clientip':        'c-ip',
    'client':          'c-ip',
    'ip':              'c-ip',
    'username':        'cs-username',
    'user':            'cs-username',
    'useragent':       'cs(user-agent)',
    'agent':           'cs(user-agent)',
    'browser':         'cs(user-agent)',
    'referer':         'cs(referer)',
    'referrer':        'cs(referer)',
    'ref':             'cs(referer)',
    'timetaken':       'time-taken',
    'duration':        'time-taken',
    'ms':              'time-taken',
    'responsetime':    'time-taken',
  };

  private resolveField(field: string): string {
    return SqlFilterService.ALIASES[field.toLowerCase()] ?? field;
  }

  private getFieldValue(field: string, entry: LogEntry): string {
    const resolved = this.resolveField(field);
    const f = resolved.toLowerCase();
    if (f === 'timestamp') return entry.timestamp ?? '';
    if (f === 'level')     return entry.level ?? '';
    if (f === 'message' || f === 'cs-uri-stem') return entry.message ?? '';

    // Try exact match first, then lowercase
    return entry.fields?.[resolved]
        ?? entry.fields?.[f]
        ?? entry.fields?.[field]
        ?? '';
  }

  /** Returns all supported field aliases for autocomplete hints. */
  getFieldHints(): string[] {
    return [
      'Status', 'SubStatus', 'Win32', 'Method', 'URI',
      'Query', 'Port', 'ServerIP', 'ClientIP', 'Username',
      'UserAgent', 'Referer', 'TimeTaken', 'Level', 'Timestamp',
    ];
  }

  private evalCompare(field: string, op: string, expected: string, entry: LogEntry): boolean {
    const actual = this.getFieldValue(field, entry);
    const numA = parseFloat(actual);
    const numE = parseFloat(expected);
    const numeric = !isNaN(numA) && !isNaN(numE);

    switch (op) {
      case '=':  return numeric ? numA === numE : actual.toLowerCase() === expected.toLowerCase();
      case '!=':
      case '<>': return numeric ? numA !== numE : actual.toLowerCase() !== expected.toLowerCase();
      case '>':  return numeric ? numA > numE  : actual > expected;
      case '>=': return numeric ? numA >= numE : actual >= expected;
      case '<':  return numeric ? numA < numE  : actual < expected;
      case '<=': return numeric ? numA <= numE : actual <= expected;
    }
    return false;
  }

  private evalLike(field: string, pattern: string, negate: boolean, entry: LogEntry): boolean {
    const actual  = this.getFieldValue(field, entry).toLowerCase();
    const escaped = pattern.replace(/[.+^${}()|[\]\\]/g, '\\$&').replace(/%/g, '.*').replace(/_/g, '.');
    const matched = new RegExp(`^${escaped}$`, 'i').test(actual);
    return negate ? !matched : matched;
  }
}
