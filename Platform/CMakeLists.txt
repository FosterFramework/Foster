cmake_minimum_required(VERSION 3.14)
project(FosterPlatform C)

# Platform Variables
option(FOSTER_OPENGL_ENABLED "Make OpenGL Renderer available" ON)
if (WIN32)
	option(FOSTER_D3D11_ENABLED "Make D3D11 Renderer available" ON)
endif()

# Set flag for building a universal binary on macOS 
if(APPLE)
	set(CMAKE_OSX_ARCHITECTURES "arm64;x86_64")
endif()

# Default to Release build for library
if(NOT CMAKE_BUILD_TYPE)
	set(CMAKE_BUILD_TYPE Release)
endif()

# Define target name
set (TARGET_NAME FosterPlatform)

add_library(${TARGET_NAME} SHARED
	include/foster_platform.h
	src/foster_platform.c
	src/foster_image.c
	src/foster_renderer.c
	src/foster_renderer_d3d11.c
	src/foster_renderer_opengl.c
)

target_include_directories(${TARGET_NAME}
	PUBLIC $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
	PRIVATE $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/src>
)

# Get platform target
if (APPLE)
	set(FosterTarget osx)
elseif (WIN32)
	set(FosterTarget x64)
elseif (UNIX)
	set(FosterTarget lib64)
else()
	message(FATAL_ERROR "Unknown Target!")
endif()

# Output libs to platform-specific library
set_target_properties(${TARGET_NAME}
	PROPERTIES
	ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	ARCHIVE_OUTPUT_DIRECTORY_RELEASE "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	LIBRARY_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	LIBRARY_OUTPUT_DIRECTORY_RELEASE "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
	RUNTIME_OUTPUT_DIRECTORY_RELEASE "${CMAKE_CURRENT_SOURCE_DIR}/libs/${FosterTarget}"
)

if (MSVC)
    # Set warning level 3
    target_compile_options(${TARGET_NAME} PRIVATE /W3)

    # Warnings as Errors Enabled
    target_compile_options(${TARGET_NAME} PRIVATE /WX)
endif()

if(WIN32)
    target_compile_definitions(${TARGET_NAME} PRIVATE _UNICODE UNICODE _CRT_SECURE_NO_WARNINGS)
    target_compile_definitions(${TARGET_NAME} PRIVATE NOMINMAX)
endif ()

# tracks which libraries we need to link, depends on Options above
set(LIBS "")

# use the OpenGL Renderer Backend
if (FOSTER_OPENGL_ENABLED)
	target_compile_definitions(${TARGET_NAME} PRIVATE FOSTER_OPENGL_ENABLED)
endif()

# use the D3D11 Renderer Backend
if (FOSTER_D3D11_ENABLED)
	target_compile_definitions(${TARGET_NAME} PRIVATE FOSTER_D3D11_ENABLED)
	set(LIBS ${LIBS} d3d11.lib dxguid.lib D3Dcompiler.lib)
endif()

# Emscripten can import SDL2 directly
if (EMSCRIPTEN)
	
	set_target_properties(${TARGET_NAME} PROPERTIES COMPILE_FLAGS "-s USE_SDL=2")
	target_link_libraries(${TARGET_NAME} "-s USE_SDL=2 -s USE_WEBGL2=1")

# Pull SDL2 from its Github repo
else()
	if (NOT DEFINED FOSTER_SDL2_LIBS)
		include(FetchContent)
		set(FETCHCONTENT_QUIET FALSE)
		set(SDL_TEST_ENABLED_BY_DEFAULT OFF)
		set(SDL_SHARED_ENABLED_BY_DEFAULT OFF)
		set(SDL_STATIC_ENABLED_BY_DEFAULT ON)

		FetchContent_Declare(
			SDL2
			GIT_REPOSITORY https://github.com/libsdl-org/SDL
			GIT_TAG release-2.28.5
			GIT_PROGRESS TRUE
		)
		FetchContent_MakeAvailable(SDL2)
		set(FOSTER_SDL2_LIBS SDL2-static)
		set(FOSTER_SDL2_INCLUDE ${sdl2_SOURCE_DIRS}/include)
	endif()

	# statically link SDL2 since we're building it ourselve
	set(LIBS ${LIBS} ${FOSTER_SDL2_LIBS})
	target_include_directories(${TARGET_NAME} PRIVATE ${FOSTER_SDL2_INCLUDE})
endif()

# Tell SDL it's going to be a shared lib
set_property(TARGET ${FOSTER_SDL2_LIBS} PROPERTY POSITION_INDEPENDENT_CODE ON)

# Link SDL
target_link_libraries(${TARGET_NAME} PRIVATE ${LIBS})
