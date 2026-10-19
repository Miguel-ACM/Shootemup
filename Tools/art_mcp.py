"""Small CLI for the installed Blender and Unity MCP servers."""
import argparse
import asyncio
import json
import os
from pathlib import Path
from contextlib import AsyncExitStack
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client
from mcp.client.streamable_http import streamablehttp_client


async def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("app", choices=["blender", "unity"])
    parser.add_argument("tool", nargs="?")
    parser.add_argument("--args", default="{}")
    parser.add_argument("--script")
    parser.add_argument("--schemas", action="store_true")
    args = parser.parse_args()
    async with AsyncExitStack() as stack:
        if args.app == "blender":
            read, write = await stack.enter_async_context(stdio_client(StdioServerParameters(
                command=r"C:\Users\Miguel\.local\bin\mcp-for-blender.exe",
                env={**os.environ, "DISABLE_TELEMETRY": "true", "PYTHONUTF8": "1", "BLENDER_HOST": "127.0.0.1"})))
        else:
            read, write, _ = await stack.enter_async_context(streamablehttp_client("http://127.0.0.1:8080/mcp"))
        session = await stack.enter_async_context(ClientSession(read, write))
        await session.initialize()
        if args.tool:
            parameters = json.loads(args.args)
            if args.app == "blender":
                parameters.setdefault("user_prompt", "ok start with the player ship in blender, psx style under 300 triangles, gray and industrial, with UV unwrapping and textures, then put it in unity")
            if args.script:
                parameters["code"] = Path(args.script).read_text(encoding="utf-8")
            result = await session.call_tool(args.tool, parameters)
            print(result.model_dump_json())
            if result.isError:
                raise RuntimeError("MCP call failed")
        else:
            result = await session.list_tools()
            for tool in result.tools:
                print(json.dumps(tool.model_dump() if args.schemas else {"name": tool.name, "description": tool.description[:180]}))


asyncio.run(asyncio.wait_for(main(), timeout=180))
