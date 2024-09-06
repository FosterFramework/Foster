cmake_minimum_required(VERSION 3.14)
project(FosterPlatform C)

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
)

target_include_directories(${TARGET_NAME}
	PUBLIC $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
	PRIVATE $<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/src>
)

# Get platform target
if (FOSTER_OVERRIDE_TARGET)
	set(FosterTarget ${FOSTER_OVERRIDE_TARGET})
elseif (APPLE)
	set(FosterTarget osx)
elseif (WIN32)
	set(FosterTarget win-x64)
elseif (UNIX)
	if(CMAKE_SYSTEM_PROCESSOR STREQUAL "aarch64")
		set(FosterTarget linux-arm64)
	elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "armv7l")
		set(FosterTarget linux-arm32)
	elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL "armv6l")
		set(FosterTarget linux-arm32)
	else()
		set(FosterTarget linux-x64)
	endif()
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

# Emscripten can import SDL3 directly
if (EMSCRIPTEN)
	
	set_target_properties(${TARGET_NAME} PROPERTIES COMPILE_FLAGS "-s USE_SDL=2")
	target_link_libraries(${TARGET_NAME} "-s USE_SDL=2 -s USE_WEBGL2=1")

# Pull SDL3 from its Github repo
else()
	if (NOT DEFINED FOSTER_SDL3_LIBS)
		include(FetchContent)
		set(FETCHCONTENT_QUIET FALSE)
		set(SDL_TEST_LIBRARY OFF)
		set(SDL_SHARED OFF)
		set(SDL_STATIC ON)

		FetchContent_Declare(
			SDL3
			GIT_REPOSITORY https://github.com/libsdl-org/sdl
			GIT_TAG 379aea5
			GIT_PROGRESS TRUE
		)
		FetchContent_MakeAvailable(SDL3)
		set(FOSTER_SDL3_LIBS SDL3-static)
		set(FOSTER_SDL3_INCLUDE ${SDL3_SOURCE_DIRS}/include)
	endif()

	# statically link SDL3 since we're building it ourselve
	set(LIBS ${LIBS} ${FOSTER_SDL3_LIBS})
	target_include_directories(${TARGET_NAME} PRIVATE ${FOSTER_SDL3_INCLUDE})
endif()

# Tell SDL it's going to be a shared lib
set_property(TARGET ${FOSTER_SDL3_LIBS} PROPERTY POSITION_INDEPENDENT_CODE ON)

# Link SDL3
target_link_libraries(${TARGET_NAME} PRIVATE ${LIBS})
